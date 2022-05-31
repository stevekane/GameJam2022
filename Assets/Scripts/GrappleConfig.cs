using UnityEngine;

[CreateAssetMenu(fileName = "Grapple Config", menuName = "Grapple/Config")]
public class GrappleConfig : ScriptableObject {
  public float FlightDuration = 2;
  public float FlightSpeed = 100;
  public float PullSpeed = 100;
  public float FlySpeed = 100;
}