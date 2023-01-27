using UnityEngine;

public class UMotionRotationCorrector : MonoBehaviour {
  public float YRotation;
  public Transform HipBone;
  public void Rotate() {
    HipBone.Rotate(new Vector3(0, YRotation, 0), Space.World);
  }
}