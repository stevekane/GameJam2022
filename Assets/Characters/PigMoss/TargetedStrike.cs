using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TargetedStrike : MonoBehaviour {
  public GameObject SpawnPrefab;
  public float MinRadius = 0;
  public float MaxRadius = 1;
  public float MinAlpha = 0;
  public float MaxAlpha = 1;
  public AnimationCurve Radius = AnimationCurve.Constant(0,1,1);
  public AnimationCurve Alpha = AnimationCurve.Constant(0,1,1);
  public Timeval Duration = Timeval.FromSeconds(1);
  public DecalProjector Projector;
}