using UnityEngine;

public class Energy : MonoBehaviour {
  public float Points;
  public float MaxPoints;
  public float OrbitPeriod = 3;
  public float OrbitRadius = .75f;
  public float OscillationAmplitude = .25f;
  public float OscillationPeriod = 1;
  public float MaxMoveSpeed = 1;
  public float MaxTurnSpeed = 360;
  public Animator[] Balls;
  public bool UseExponentialLerp = true;
  public float ExponentialLerpLambda = 1;

  public void Add(float amount) => Points = Mathf.Min(MaxPoints, Points + amount);
  public void Consume(float amount) => Points = Mathf.Max(0, Points - amount);

  void Start() {
    foreach (var ball in Balls) {
      ball.SetBool("Active", false);
      ball.transform.SetParent(null);
    }
  }

  void OnDestroy() {
    foreach (var ball in Balls) {
      if (ball) {
        Destroy(ball);
      }
    }
  }

  public static Vector3 ExponentialLerp(Vector3 a, Vector3 b, float lambda, float dt) {
    return Vector3.Lerp(a, b, 1 - Mathf.Exp(-lambda * dt));
  }

  /*
  I want orbiting to be fully independent of the orientation of the owner.
  */

  void Update() {
    int numActive = (int)Points;
    var rotation = 2*Mathf.PI * Time.time / OrbitPeriod;
    for (int i = 0; i < numActive; i++) {
      var fraction = (float)i/(float)numActive;
      var angle = fraction*Mathf.PI*2+rotation;
      var x = OrbitRadius * Mathf.Cos(angle);
      var y = OscillationAmplitude * Mathf.Sin(2.0f * Mathf.PI * Time.time / OscillationPeriod + i);
      var z = OrbitRadius * Mathf.Sin(angle);
      var pt = transform.position + new Vector3(x, y, z);
      var tocenter = (Balls[i].transform.position - transform.position).normalized;
      Balls[i].SetBool("Active", true);
      if (UseExponentialLerp) {
        Balls[i].transform.position = ExponentialLerp(Balls[i].transform.position, pt, ExponentialLerpLambda, Time.deltaTime);
      } else {
        Balls[i].transform.position = Vector3.MoveTowards(Balls[i].transform.position, pt, MaxMoveSpeed*Time.deltaTime);
      }
      if (tocenter.sqrMagnitude > 0)
        Balls[i].transform.rotation = Quaternion.RotateTowards(Balls[i].transform.rotation, Quaternion.LookRotation(tocenter), MaxTurnSpeed*Time.deltaTime);
    }
    for (int i = numActive; i < Balls.Length; i++) {
      Balls[i].SetBool("Active", false);
      if (UseExponentialLerp) {
        Balls[i].transform.position = ExponentialLerp(Balls[i].transform.position, transform.position, ExponentialLerpLambda, Time.deltaTime);
      } else {
        Balls[i].transform.position = Vector3.MoveTowards(Balls[i].transform.position, transform.position, MaxMoveSpeed*Time.deltaTime);
      }
    }
  }
}