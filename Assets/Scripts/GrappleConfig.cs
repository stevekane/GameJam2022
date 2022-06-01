using UnityEngine;

[CreateAssetMenu(fileName = "Grapple Config", menuName = "Grapple/Config")]
public class GrappleConfig : ScriptableObject {
  public float FlightDuration = 2;
  public float FlightSpeed = 100;
  public float ZipSpeed = 100;
  public float MinZippingActionRadius = 1f;
  public float MaxZippingActionRadius = 3f;
}