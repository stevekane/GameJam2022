using UnityEngine;

public class Defaults : MonoBehaviour {
  public static Defaults Instance;

  [Header("Combat")]
  public AnimationCurve HitStopLocalTime;

  [Header("Physics")]
  public LayerMask EnvironmentLayerMask;
  public LayerMask GrapplePointLayerMask;
  public string GroundTag = "Ground";

  public bool ShowAltimeter = false;
}