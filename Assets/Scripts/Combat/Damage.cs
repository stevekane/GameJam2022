using UnityEngine;

public class Damage : MonoBehaviour {
  Optional<Status> Status;
  [SerializeField] float _Points;
  public float Points { get => _Points; private set => _Points = value; }

  void Awake() {
    Status = GetComponent<Status>();
  }

  public void AddPoints(float dp) {
    if (Status?.Value.IsDamageable ?? true)
      Points += dp;
  }
}