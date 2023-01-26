using UnityEngine;

public class Energy : MonoBehaviour {
  public float Points;
  public float MaxPoints;
  public float OrbitSpeed = 180;
  public float OrbitRadius = .75f;
  public float OscillationAmplitude = .25f;
  public float OscillationPeriod = 1;
  public float MaxMoveSpeed = 1;
  public float MaxTurnSpeed = 360;
  public Animator[] Balls;

  public void Add(float amount) => Points = Mathf.Min(MaxPoints, Points + amount);
  public void Consume(float amount) => Points = Mathf.Max(0, Points - amount);

  void Awake() => Balls.ForEach(o => o.SetBool("Active", false));

  void Update() {
    transform.Rotate(new(0, OrbitSpeed * Time.deltaTime, 0));
    int numActive = (int)Points;
    for (int i = 0; i < numActive; i++) {
      var fraction = (float)i/(float)numActive;
      var angle = fraction*Mathf.PI*2;
      var x = OrbitRadius * Mathf.Cos(angle);
      var y = OscillationAmplitude * Mathf.Sin(2.0f * Mathf.PI * Time.time / OscillationPeriod + i);
      var z = OrbitRadius * Mathf.Sin(angle);
      var pt = transform.position + transform.TransformVector(new Vector3(x, y, z));
      var tocenter = (Balls[i].transform.position - transform.position).normalized;
      Balls[i].SetBool("Active", true);
      Balls[i].transform.position = Vector3.MoveTowards(Balls[i].transform.position, pt, MaxMoveSpeed*Time.deltaTime);
      if (tocenter.sqrMagnitude > 0)
        Balls[i].transform.rotation = Quaternion.RotateTowards(Balls[i].transform.rotation, Quaternion.LookRotation(tocenter), MaxTurnSpeed*Time.deltaTime);
    }
    for (int i = numActive; i < Balls.Length; i++) {
      Balls[i].SetBool("Active", false);
      Balls[i].transform.position = Vector3.MoveTowards(Balls[i].transform.position, transform.position, MaxMoveSpeed*Time.deltaTime);
    }
  }
}