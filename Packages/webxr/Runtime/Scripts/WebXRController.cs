using UnityEngine;
#if UNITY_EDITOR || !UNITY_WEBGL
using UnityEngine.XR;
#endif
using System;
using System.Collections.Generic;

namespace WebXR
{

  public class WebXRController : MonoBehaviour
  {
    public enum ButtonTypes
    {
      Trigger = 0,
      Grip = 1,
      Thumbstick = 2,
      Touchpad = 3,
      ButtonA = 4,
      ButtonB = 5
    }

    public enum AxisTypes
    {
      Trigger,
      Grip
    }

    public enum Axis2DTypes
    {
      Thumbstick, // primary2DAxis
      Touchpad // secondary2DAxis
    }

    public Action<bool> OnControllerActive;
    public Action<bool> OnHandActive;
    public Action<WebXRHandData> OnHandUpdate;

    [Tooltip("Controller hand to use.")]
    public WebXRControllerHand hand = WebXRControllerHand.NONE;
    [Tooltip("Simulate 3dof controller")]
    public bool simulate3dof = false;
    [Tooltip("Vector from head to elbow")]
    public Vector3 eyesToElbow = new Vector3(0.1f, -0.4f, 0.15f);
    [Tooltip("Vector from elbow to hand")]
    public Vector3 elbowHand = new Vector3(0, 0, 0.25f);

    private Matrix4x4 sitStand;

    private float trigger;
    private float squeeze;
    private float thumbstick;
    private float thumbstickX;
    private float thumbstickY;
    private float touchpad;
    private float touchpadX;
    private float touchpadY;
    private float buttonA;
    private float buttonB;

    private Quaternion headRotation;
    private Vector3 headPosition;
    private Dictionary<ButtonTypes, WebXRControllerButton> buttonStates = new Dictionary<ButtonTypes, WebXRControllerButton>();

    private bool controllerActive = false;
    private bool handActive = false;

    private string[] profiles = null;

#if UNITY_EDITOR || !UNITY_WEBGL
    private InputDeviceCharacteristics xrHand = InputDeviceCharacteristics.Controller;
    private InputDevice? inputDevice;
    private HapticCapabilities? hapticCapabilities;
    private int buttonsFrameUpdate = -1;

    private void Update()
    {
      TryUpdateButtons();
    }
#endif

    private void TryUpdateButtons()
    {
#if UNITY_EDITOR || !UNITY_WEBGL
      if (buttonsFrameUpdate == Time.frameCount)
      {
        return;
      }
      buttonsFrameUpdate = Time.frameCount;
      if (!WebXRManager.Instance.isSubsystemAvailable && inputDevice != null)
      {
        inputDevice.Value.TryGetFeatureValue(CommonUsages.trigger, out trigger);
        inputDevice.Value.TryGetFeatureValue(CommonUsages.grip, out squeeze);
        if (trigger <= 0.02)
        {
          trigger = 0;
        }
        else if (trigger >= 0.98)
        {
          trigger = 1;
        }

        if (squeeze <= 0.02)
        {
          squeeze = 0;
        }
        else if (squeeze >= 0.98)
        {
          squeeze = 1;
        }

        Vector2 axis2D;
        if (inputDevice.Value.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis2D))
        {
          thumbstickX = axis2D.x;
          thumbstickY = axis2D.y;
        }
        if (inputDevice.Value.TryGetFeatureValue(CommonUsages.secondary2DAxis, out axis2D))
        {
          touchpadX = axis2D.x;
          touchpadY = axis2D.y;
        }
        bool buttonPressed;
        if (inputDevice.Value.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out buttonPressed))
        {
          thumbstick = buttonPressed ? 1 : 0;
        }
        if (inputDevice.Value.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out buttonPressed))
        {
          touchpad = buttonPressed ? 1 : 0;
        }
        if (inputDevice.Value.TryGetFeatureValue(CommonUsages.primaryButton, out buttonPressed))
        {
          buttonA = buttonPressed ? 1 : 0;
        }
        if (inputDevice.Value.TryGetFeatureValue(CommonUsages.secondaryButton, out buttonPressed))
        {
          buttonB = buttonPressed ? 1 : 0;
        }

        WebXRControllerButton[] buttons = new WebXRControllerButton[6];
        buttons[(int)ButtonTypes.Trigger] = new WebXRControllerButton(trigger == 1, trigger);
        buttons[(int)ButtonTypes.Grip] = new WebXRControllerButton(squeeze == 1, squeeze);
        buttons[(int)ButtonTypes.Thumbstick] = new WebXRControllerButton(thumbstick == 1, thumbstick);
        buttons[(int)ButtonTypes.Touchpad] = new WebXRControllerButton(touchpad == 1, touchpad);
        buttons[(int)ButtonTypes.ButtonA] = new WebXRControllerButton(buttonA == 1, buttonA);
        buttons[(int)ButtonTypes.ButtonB] = new WebXRControllerButton(buttonB == 1, buttonB);
        UpdateButtons(buttons);
      }
#endif
    }

    // Updates button states from Web gamepad API.
    private void UpdateButtons(WebXRControllerButton[] buttons)
    {
      for (int i = 0; i < buttons.Length; i++)
      {
        WebXRControllerButton button = buttons[i];
        SetButtonState((ButtonTypes)i, button.pressed, button.value);
      }
    }

    public float GetAxis(AxisTypes action)
    {
      TryUpdateButtons();
      switch (action)
      {
        case AxisTypes.Grip:
          return squeeze;
        case AxisTypes.Trigger:
          return trigger;
      }
      return 0;
    }

    public Vector2 GetAxis2D(Axis2DTypes action)
    {
      TryUpdateButtons();
      switch (action)
      {
        case Axis2DTypes.Thumbstick:
          return new Vector2(thumbstickX, thumbstickY);
        case Axis2DTypes.Touchpad:
          return new Vector2(touchpadX, touchpadY);
      }
      return Vector2.zero;
    }

    public bool GetButton(ButtonTypes action)
    {
      TryUpdateButtons();
      if (!buttonStates.ContainsKey(action))
      {
        return false;
      }
      return buttonStates[action].pressed;
    }

    private void SetButtonState(ButtonTypes action, bool isPressed, float value)
    {
      if (buttonStates.ContainsKey(action))
      {
        buttonStates[action].UpdateState(isPressed, value);
      }
      else
        buttonStates.Add(action, new WebXRControllerButton(isPressed, value));
    }

    public bool GetButtonDown(ButtonTypes action)
    {
      TryUpdateButtons();
      if (!buttonStates.ContainsKey(action))
      {
        return false;
      }
      return buttonStates[action].down;
    }

    public bool GetButtonUp(ButtonTypes action)
    {
      TryUpdateButtons();
      if (!buttonStates.ContainsKey(action))
      {
        return false;
      }
      return buttonStates[action].up;
    }

    public float GetButtonIndexValue(int index)
    {
      TryUpdateButtons();
      switch (index)
      {
        case 0:
          return trigger;
        case 1:
          return squeeze;
        case 2:
          return touchpad;
        case 3:
          return thumbstick;
        case 4:
          return buttonA;
        case 5:
          return buttonB;
      }
      return 0;
    }

    public float GetAxisIndexValue(int index)
    {
      TryUpdateButtons();
      switch (index)
      {
        case 0:
          return touchpadX;
        case 1:
          return touchpadY;
        case 2:
          return thumbstickX;
        case 3:
          return thumbstickY;
      }
      return 0;
    }

    public string[] GetProfiles()
    {
      return profiles;
    }

    private void OnHeadsetUpdate(Matrix4x4 leftProjectionMatrix,
        Matrix4x4 rightProjectionMatrix,
        Matrix4x4 leftViewMatrix,
        Matrix4x4 rightViewMatrix,
        Matrix4x4 sitStandMatrix)
    {
      Matrix4x4 trs = WebXRMatrixUtil.TransformViewMatrixToTRS(leftViewMatrix);
      this.headRotation = WebXRMatrixUtil.GetRotationFromMatrix(trs);
      this.headPosition = WebXRMatrixUtil.GetTranslationFromMatrix(trs);
      this.sitStand = sitStandMatrix;
    }

    private void OnControllerUpdate(WebXRControllerData controllerData)
    {
      if (controllerData.hand == (int)hand)
      {
        if (!controllerData.enabled)
        {
          SetControllerActive(false);
          return;
        }

        profiles = controllerData.profiles;

        transform.localRotation = controllerData.rotation;
        transform.localPosition = controllerData.position;

        trigger = controllerData.trigger;
        squeeze = controllerData.squeeze;
        thumbstick = controllerData.thumbstick;
        thumbstickX = controllerData.thumbstickX;
        thumbstickY = controllerData.thumbstickY;
        touchpad = controllerData.touchpad;
        touchpadX = controllerData.touchpadX;
        touchpadY = controllerData.touchpadY;
        buttonA = controllerData.buttonA;
        buttonB = controllerData.buttonB;

        WebXRControllerButton[] buttons = new WebXRControllerButton[6];
        buttons[(int)ButtonTypes.Trigger] = new WebXRControllerButton(trigger == 1, trigger);
        buttons[(int)ButtonTypes.Grip] = new WebXRControllerButton(squeeze == 1, squeeze);
        buttons[(int)ButtonTypes.Thumbstick] = new WebXRControllerButton(thumbstick == 1, thumbstick);
        buttons[(int)ButtonTypes.Touchpad] = new WebXRControllerButton(touchpad == 1, touchpad);
        buttons[(int)ButtonTypes.ButtonA] = new WebXRControllerButton(buttonA == 1, buttonA);
        buttons[(int)ButtonTypes.ButtonB] = new WebXRControllerButton(buttonB == 1, buttonB);
        UpdateButtons(buttons);

        SetControllerActive(true);
      }
    }

    private void OnHandUpdateInternal(WebXRHandData handData)
    {
      if (handData.hand == (int)hand)
      {
        if (!handData.enabled)
        {
          SetHandActive(false);
          return;
        }
        SetControllerActive(false);
        SetHandActive(true);

        transform.localPosition = handData.joints[0].position;
        transform.localRotation = handData.joints[0].rotation;

        Quaternion rotationOffset = Quaternion.Inverse(handData.joints[0].rotation);

        trigger = handData.trigger;
        squeeze = handData.squeeze;

        WebXRControllerButton[] buttons = new WebXRControllerButton[2];
        buttons[(int)ButtonTypes.Trigger] = new WebXRControllerButton(trigger == 1, trigger);
        buttons[(int)ButtonTypes.Grip] = new WebXRControllerButton(squeeze == 1, squeeze);
        UpdateButtons(buttons);

        OnHandUpdate?.Invoke(handData);
      }
    }

    private void SetControllerActive(bool active)
    {
      if (controllerActive != active)
      {
        controllerActive = active;
        OnControllerActive?.Invoke(controllerActive);
      }
    }

    private void SetHandActive(bool active)
    {
      if (handActive == active)
      {
        return;
      }
      handActive = active;
      OnHandActive?.Invoke(handActive);
    }

    // intensity 0 to 1, duration milliseconds
    public void Pulse(float intensity, float durationMilliseconds)
    {
      if (WebXRManager.Instance.isSubsystemAvailable)
      {
        WebXRManager.Instance.HapticPulse(hand, intensity, durationMilliseconds);
      }
#if UNITY_EDITOR || !UNITY_WEBGL
      else if (inputDevice != null && hapticCapabilities != null
               && hapticCapabilities.Value.supportsImpulse)
      {
        // duration in seconds
        inputDevice.Value.SendHapticImpulse(0, intensity, durationMilliseconds * 0.001f);
      }
#endif
    }

    void OnEnable()
    {
      WebXRManager.OnControllerUpdate += OnControllerUpdate;
      WebXRManager.OnHandUpdate += OnHandUpdateInternal;
      WebXRManager.OnHeadsetUpdate += OnHeadsetUpdate;
      SetControllerActive(false);
      SetHandActive(false);
#if UNITY_EDITOR || !UNITY_WEBGL
      switch (hand)
      {
        case WebXRControllerHand.LEFT:
          xrHand = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left;
          break;
        case WebXRControllerHand.RIGHT:
          xrHand = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right;
          break;
      }

      List<InputDevice> allDevices = new List<InputDevice>();
      InputDevices.GetDevicesWithCharacteristics(xrHand, allDevices);
      foreach (InputDevice device in allDevices)
      {
        HandleInputDevicesConnected(device);
      }

      InputDevices.deviceConnected += HandleInputDevicesConnected;
      InputDevices.deviceDisconnected += HandleInputDevicesDisconnected;
#endif
    }

    void OnDisabled()
    {
      WebXRManager.OnControllerUpdate -= OnControllerUpdate;
      WebXRManager.OnHandUpdate -= OnHandUpdateInternal;
      WebXRManager.OnHeadsetUpdate -= OnHeadsetUpdate;
      SetControllerActive(false);
      SetHandActive(false);
#if UNITY_EDITOR || !UNITY_WEBGL
      InputDevices.deviceConnected -= HandleInputDevicesConnected;
      InputDevices.deviceDisconnected -= HandleInputDevicesDisconnected;
      inputDevice = null;
#endif
    }

#if UNITY_EDITOR || !UNITY_WEBGL
    private void HandleInputDevicesConnected(InputDevice device)
    {
      if (device.characteristics.HasFlag(xrHand))
      {
        inputDevice = device;
        HapticCapabilities capabilities;
        if (device.TryGetHapticCapabilities(out capabilities))
        {
          hapticCapabilities = capabilities;
        }
        profiles = null;
        // TODO: Find a better way to get device profile
        if (device.manufacturer == "Oculus")
        {
          profiles = new string[]{"oculus-touch-v2"};
        }
        else
        {
          string profileName = "generic";
          bool addedFeatures = false;
          float tempFloat = 0;
          Vector2 tempVec2 = Vector2.zero;
          if (device.TryGetFeatureValue(CommonUsages.trigger, out tempFloat))
          {
            profileName += "-trigger";
            addedFeatures = true;
          }
          if (device.TryGetFeatureValue(CommonUsages.grip, out tempFloat))
          {
            profileName += "-squeeze";
            addedFeatures = true;
          }
          if (device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out tempVec2))
          {
            profileName += "-touchpad";
            addedFeatures = true;
          }
          if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out tempVec2))
          {
            profileName += "-thumbstick";
            addedFeatures = true;
          }
          if (!addedFeatures)
          {
            profileName += "-button";
          }
          profiles = new string[]{profileName};
        }
        TryUpdateButtons();
        SetControllerActive(true);
      }
    }

    private void HandleInputDevicesDisconnected(InputDevice device)
    {
      if (inputDevice != null && inputDevice.Value == device)
      {
        inputDevice = null;
        hapticCapabilities = null;
        SetControllerActive(false);
      }
    }
#endif
  }
}
