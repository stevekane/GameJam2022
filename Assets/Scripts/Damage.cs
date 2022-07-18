using UnityEngine;

public class Damage : MonoBehaviour {
  public float Points { get; private set; }

  public void AddPoints(float dp) {
    Points += dp;
  }

  public void SetPoints(float p) {
    Points = p;
  }
}