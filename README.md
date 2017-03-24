# Unity-VR-InputModule
A very basic input module for laser pointer style controller-UI interaction.

Contents:
VRControllerInputModule - an input module for Unity event system that enables point and click functionality for VR controllers.
VRInputManager - helper class for wrapping controller parameters (button state, etc.)

# How to
1. Attach a camera to your controller;
2. Set the camera's culling mask to none;
3. Assign VRControllerInputModule to the EventSystem;
4. Assign the camera to VRControllerInputMode's Ui Camera;
5. Assign the camera as Event Camera to all of the canvases;
6. (For click to work) Call SetIsControllerButtonPressed on your controller's button press.

# Upsides
Very simple and short module. You can click and drag all you want using standard Unity UI. Hardware-independent.

# Downsides
1) Panels are hit by raycast, but canvases are not. On a panel, you can drag a slider while pointing at the panel, but on a canvas you can only drag it while pointing at it.
2) A button will remain hovered if it was pressed and the controller moved away and released outside of the button.


I'm going to fix that later, but no promises:)

For more details please see my post: http://codrspace.com/Sergey-Shamov/laser-pointer-vr-ui/
