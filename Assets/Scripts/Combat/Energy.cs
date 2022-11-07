using UnityEngine;

public class Energy : MonoBehaviour {
  [SerializeField] public float Points;
  [SerializeField] public float MaxPoints;

  public void Add(float amount) => Points = Mathf.Min(MaxPoints, Points + amount);
  public void Consume(float amount) => Points = Mathf.Max(0, Points - amount);
}