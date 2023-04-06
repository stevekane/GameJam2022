using UnityEngine;

public class Defaults : MonoBehaviour {
  public static Defaults Instance;

  void Awake() {
    if (Instance) {
      Destroy(this);
    } else {
      Instance = this;
    }
  }

  [Header("Combat")]
  public AnimationCurve HitStopLocalTime;
  public AvatarMask WholeBodyMask;
  public AvatarMask UpperBodyMask;

  [Header("Physics")]
  public LayerMask EnvironmentLayerMask;
  public LayerMask BuildingLayerMask;
  public LayerMask GrapplePointLayerMask;
  public string GroundTag = "Ground";

  public bool ShowAltimeter = false;
}