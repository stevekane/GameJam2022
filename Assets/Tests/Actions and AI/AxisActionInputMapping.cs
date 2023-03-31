using UnityEngine;

public enum AxisProcessor {
  XY,
  RawXY,
  XZ,
  RawXZ,
  FromPersonalCamera
}

public class AxisActionInputMapping : MonoBehaviour {
  [SerializeField] AxisCode AxisCode;
  [SerializeField] ActionEventSourceVector3 Action;
  [SerializeField] AxisProcessor AxisProcessor;
  [SerializeField] PersonalCamera PersonalCamera;
  void Fire(AxisState axisState) {
    switch (AxisProcessor) {
      case AxisProcessor.XY:
        Action.Fire(axisState.XY);
      break;
      case AxisProcessor.RawXY:
        Action.Fire(axisState.RawXY);
      break;
      case AxisProcessor.XZ:
        Action.Fire(axisState.XZ);
      break;
      case AxisProcessor.RawXZ:
        Action.Fire(axisState.RawXZ);
      break;
      case AxisProcessor.FromPersonalCamera:
        Action.Fire(axisState.XZFrom(PersonalCamera.Current));
      break;
    }
  }
  void OnEnable() {
    GetComponentInParent<InputManager>()
    .AxisEvent(AxisCode)
    .Listen(Fire);
  }
  void OnDisable() {
    GetComponentInParent<InputManager>()
    ?.AxisEvent(AxisCode)
    .Unlisten(Fire);
  }
}