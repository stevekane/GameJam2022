using UnityEngine;

public class Energy : MonoBehaviour {
  public float Points;
  public float MaxPoints;
  public float SpinSpeed;
  public GameObject[] Balls;

  public void Add(float amount) => Points = Mathf.Min(MaxPoints, Points + amount);
  public void Consume(float amount) => Points = Mathf.Max(0, Points - amount);

  void Awake() => Balls.ForEach(o => o.SetActive(false));
  void FixedUpdate() {
    transform.Rotate(new(0, SpinSpeed * Time.fixedDeltaTime, 0));
    int numActive = (int)Points;
    for (int i = 0; i < Balls.Length; i++)
      Balls[i].SetActive(i < numActive);
  }
}