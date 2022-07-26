using UnityEngine;

public class Damage : MonoBehaviour {
  [SerializeField] float _Points;
  public float Points { get => _Points; private set => _Points = value; }

  public void AddPoints(float dp) {
    Points += dp;
  }

  public void SetPoints(float p) {
    Points = p;
  }
}