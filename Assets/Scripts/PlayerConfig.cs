using UnityEngine;

[CreateAssetMenu(fileName = "Player Config", menuName = "Player/Config")]
public class PlayerConfig : ScriptableObject {
  public float MoveSpeed = 10f;
  public float GrappleSpeed = 100f;
  public float GrappleFlightDuration = 2f;
}