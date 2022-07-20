using UnityEngine;

public class Hurtbox : MonoBehaviour {
  public Defender Defender { get; set; }

  void Awake() {
    Defender = GetComponentInParent<Defender>();
  }
}