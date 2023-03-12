using UnityEngine;
using TMPro;

public class WorldSpaceMessage : MonoBehaviour {
  [SerializeField] float AlphaSmoothing = .1f;
  [SerializeField] float VelocitySmoothing = .1f;
  [SerializeField] float InitialSpeed = 5;
  [SerializeField] Timeval Duration = Timeval.FromSeconds(1);
  [SerializeField] Vector3 Velocity;
  [SerializeField] TextMeshPro Text;

  public void SetMessage(string message) {
    Text.text = message;
  }

  void Awake() {
    Destroy(gameObject, Duration.Seconds);
    Velocity.x = Random.Range(-InitialSpeed, InitialSpeed);
    Velocity.y = 0f;
    Velocity.z = 0f;
  }

  void Update() {
    var dt = Time.deltaTime;
    Velocity = Vector3.Lerp(Velocity, InitialSpeed * Vector3.up, 1 - Mathf.Pow(VelocitySmoothing, dt));
    Text.alpha = Mathf.Lerp(Text.alpha, 0, 1 - Mathf.Pow(AlphaSmoothing, dt));
    Text.transform.localPosition += dt * Velocity;
  }
}