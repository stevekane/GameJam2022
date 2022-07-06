using UnityEngine;

[ExecuteInEditMode]
public class RotateRelativeToCamera : MonoBehaviour {
  [SerializeField]
  float MAX_ROTATION_DEGREES = 30;
  [SerializeField]
  AnimationCurve RotationByOrientation;

  void LateUpdate() {
    var camera = MainCamera.Instance;
    var worldRotationAxis = Vector3.right;
    var worldForwardXZ = Vector3.forward.XZ();
    var parentForwardXZ = transform.parent.forward.XZ();
    var dot = Vector3.Dot(worldForwardXZ, parentForwardXZ);
    var rotationStrength = RotationByOrientation.Evaluate(dot);
    var degrees = rotationStrength*MAX_ROTATION_DEGREES;
    var worldRotationX = Quaternion.AngleAxis(degrees, worldRotationAxis);
    var parentRotation = transform.parent.rotation;
    var totalRotation = worldRotationX*parentRotation;
    transform.rotation = totalRotation;
  }
}