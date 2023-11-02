using UnityEngine;
using TMPro;

public class WorldSpaceMessage : MonoBehaviour {
  [SerializeField] float AlphaSmoothing = .1f;
  [SerializeField] float VelocitySmoothing = .1f;
  [SerializeField] float VelocityDamping = .98f;
  [SerializeField] Vector3 TargetVelocity = Vector3.up;
  [SerializeField] TextMeshPro Text;

  public Vector3 LocalScale;
  public Vector3 LocalVelocity;

  public string Message {
    get => Text.text;
    set => Text.text = value;
  }

  void Update() {
    var dt = Time.deltaTime;
    LocalVelocity = Vector3.Lerp(LocalVelocity, TargetVelocity, 1 - Mathf.Pow(VelocitySmoothing, dt));
    LocalVelocity *= VelocityDamping;
    Text.alpha = Mathf.Lerp(Text.alpha, 0, 1 - Mathf.Pow(AlphaSmoothing, dt));
    Text.transform.localPosition += dt * LocalVelocity;
    Text.transform.localScale = LocalScale;
  }
}