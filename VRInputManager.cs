using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VRInputManager
{
    private static bool m_isKeyPressed = false;         // true if controller button is pressed
    private static Transform m_controllerTransform;     // controller transform
    private static bool m_controllerActive = true;      // TOOD: actual value             // true if controller is active (movement should be tracked)

    public static void SetIsControllerButtonPressed(bool isPressed)
    {
        m_isKeyPressed = isPressed;
    }

    public static bool GetIsControllerButtonPressed()
    {
        return m_isKeyPressed;
    }

    public static void SetControllerTransform(Transform transform)
    {
        m_controllerTransform = transform;
    }

    public static Transform GetControllerTransform()
    {
        return m_controllerTransform;
    }

    public static void SetControllerActive(bool active)
    {
        m_controllerActive = active;
    }

    public static bool GetControllerActive()
    {
        return m_controllerActive;
    }
}
