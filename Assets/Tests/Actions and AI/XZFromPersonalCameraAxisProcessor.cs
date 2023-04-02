using UnityEngine;

public class XZFromPersonalCameraAxisProcessor : AxisProcessor {
  [SerializeField] PersonalCamera PersonalCamera;
  [SerializeField] SimpleAbilityVector3 Ability;

  public override void Process(AxisState axisState) {
    Ability.Value = PersonalCamera.CameraScreenToWorldXZ(axisState.XY);
  }
}