using UnityEngine;
using UnityEngine.EventSystems;

public class VRControllerInputModule : BaseInputModule
{
    [Tooltip("A camera mounted on the controller")]
    public Camera uiCamera;

    [Tooltip("The threshold is a square length at which the cursor will begin drag. Lenght is measured in world coordinates.")]
    public float dragThreshold = 0.1f;
    
    // debug
    public  UnityEngine.UI.Text uiDebugText;            
    private bool m_useDebugText = false;  
    private string[] m_debugStrings = new string[5];
    // debug

    private bool m_isButtonPressed = false;           // true if controller's button is currently pressed, false otherwise
    private bool m_isButtonPressedChanged = false;    // true if controller's button was pressed or released during the last frame
    private float m_pressedDistance;                // Distance the cursor travelled while pressed.

    private Vector2 m_cameraCenter;
    private Vector3 m_lastRaycastHitPoint;
    private PointerEventData m_pointerEventData;
    private bool m_isActive = false;

    protected override void Start()
    {
        base.Start();
        if (null != uiCamera)
        {
            m_isActive = true;
            m_cameraCenter = new Vector2(uiCamera.pixelWidth / 2, uiCamera.pixelHeight / 2);

            m_useDebugText = null != uiDebugText;
            WriteDebug("Camera center: " + m_cameraCenter.ToString());
        }
    }

    public override void Process()
    {
        if (m_isActive)
        {
            bool usedEvent = SendUpdateEventToSelectedObject();

            MyUpdateControllerData();
            ProcessControllerEvent();
        }
    }

    private void MyUpdateControllerData()
    {
        m_isButtonPressedChanged = false;
        if (m_isButtonPressed != VRInputManager.GetIsControllerButtonPressed())
        {
            m_isButtonPressedChanged = true;
            m_isButtonPressed = VRInputManager.GetIsControllerButtonPressed();
        }
    }

    private void ProcessControllerEvent()
    {
        PointerEventData eventData = GetPointerEventData();

        ProcessPress(eventData);
        ProcessMove(eventData);
        ProcessDrag(eventData);
    }

    private PointerEventData GetPointerEventData()
    {
        // Currently the module is made for a single controller with one button.
        // That means that we only have a single pointer.
        if (null == m_pointerEventData)
            m_pointerEventData = new PointerEventData(eventSystem);

        if (VRInputManager.GetControllerActive())
        {
            m_pointerEventData.position = m_cameraCenter;

            m_pointerEventData.scrollDelta = Vector2.zero;
            m_pointerEventData.button = PointerEventData.InputButton.Left;
            eventSystem.RaycastAll(m_pointerEventData, m_RaycastResultCache);
            var raycast = FindFirstRaycast(m_RaycastResultCache);

            // Delta is used to define if the cursor was moved.
            // We will also use it for drag threshold calculation, for which we'll store world distance 
            // between the last and the current raycasts (will actually use sqrmagnitude for its speed).
            Ray ray = new Ray(uiCamera.transform.position, uiCamera.transform.forward);
            Vector3 hitPoint = ray.GetPoint(raycast.distance);
            m_pointerEventData.delta = new Vector2((hitPoint - m_lastRaycastHitPoint).sqrMagnitude, 0);
            m_lastRaycastHitPoint = hitPoint;

            m_pointerEventData.pointerCurrentRaycast = raycast;

            // Debug
            if (m_RaycastResultCache.Count > 0)
                WriteDebug("Raycast hit " + raycast.gameObject.name);

            m_RaycastResultCache.Clear();
        }

        return m_pointerEventData;
    }

    // Copied from PointerInputModule
    private void ProcessDrag(PointerEventData eventData)
    {
        WriteDebug(eventData.delta.sqrMagnitude.ToString());

        // If pointer is not moving or if a button is not pressed (or pressed control did not return drag handler), do nothing
        if (!eventData.IsPointerMoving() || eventData.pointerDrag == null)
            return;

        // We are eligible for drag. If drag did not start yet, add drag distance
        if (!eventData.dragging)
        {
            m_pressedDistance += eventData.delta.x;

            if (ShouldStartDrag(eventData))
            {
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                eventData.dragging = true;
            }
        }

        // Drag notification
        if (eventData.dragging)
        {
            // Before doing drag we should cancel any pointer down state
            // And clear selection!
            if (eventData.pointerPress != eventData.pointerDrag)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                eventData.eligibleForClick = false;
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;
            }
            ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
        }
    }

    private bool ShouldStartDrag(PointerEventData eventData)
    {
        return !m_isButtonPressedChanged && (m_pressedDistance > dragThreshold);
    }

    // Copied from PointerInputModule
    private void ProcessMove(PointerEventData eventData)
    {
        var targetGO = eventData.pointerCurrentRaycast.gameObject;
        HandlePointerExitAndEnter(eventData, targetGO);
    }

    // modified StandaloneInputModule
    private void ProcessPress(PointerEventData eventData)
    {
        var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

        // PointerDown notification
        if (MyIsButtonPressedThisFrame())
        {
            eventData.eligibleForClick = true;
            eventData.delta = Vector2.zero;
            eventData.useDragThreshold = true;
            eventData.pressPosition = eventData.position;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, eventData);

            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            eventData.pointerPress = newPressed;    // TODO:remove?
            m_pressedDistance = 0;
            eventData.rawPointerPress = currentOverGo;

            eventData.clickTime = Time.unscaledTime;

            // Save the drag handler as well
            eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (eventData.pointerDrag != null)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
        }

        // PointerUp notification
        if (MyIsButtonReleasedThisFrame())
        {
            ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

            // see if we button up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (eventData.pointerPress == pointerUpHandler && eventData.eligibleForClick)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
            }
            else if (eventData.pointerDrag != null && eventData.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.dropHandler);
            }

            eventData.eligibleForClick = false;
            eventData.pointerPress = null;
            m_pressedDistance = 0;              // just in case
            eventData.rawPointerPress = null;

            if (eventData.pointerDrag != null && eventData.dragging)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

            eventData.dragging = false;
            eventData.pointerDrag = null;

            // redo pointer enter / exit to refresh state
            // so that if we hovered over something that ignored it before
            // due to having pressed on something else
            // it now gets it.
            if (currentOverGo != eventData.pointerEnter)
            {
                HandlePointerExitAndEnter(eventData, null);
                HandlePointerExitAndEnter(eventData, currentOverGo);
            }
        }
    }

    // Copied from PointerInputModule
    private void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
    {
        // Selection tracking
        var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
        // if we have clicked something new, deselect the old thing
        // leave 'selection handling' up to the press event though.
        if (selectHandlerGO != eventSystem.currentSelectedGameObject)
            eventSystem.SetSelectedGameObject(null, pointerEvent);
    }

    // Copied from StandaloneInputModule
    private bool SendUpdateEventToSelectedObject()
    {
        if (eventSystem.currentSelectedGameObject == null)
            return false;

        var data = GetBaseEventData();
        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
        return data.used;
    }

    private bool MyIsButtonReleasedThisFrame()
    {
        return (m_isButtonPressedChanged && !m_isButtonPressed);
    }

    private bool MyIsButtonPressedThisFrame()
    {
        return (m_isButtonPressedChanged && m_isButtonPressed);
    }

    // Debug
    private void WriteDebug(string text)
    {
        if (!m_useDebugText)
            return;

        m_debugStrings[4] = m_debugStrings[3];
        m_debugStrings[3] = m_debugStrings[2];
        m_debugStrings[2] = m_debugStrings[1];
        m_debugStrings[1] = m_debugStrings[0];
        m_debugStrings[0] = text;
        uiDebugText.text = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", m_debugStrings[0], m_debugStrings[1], m_debugStrings[2], m_debugStrings[3], m_debugStrings[4]);
    }
}