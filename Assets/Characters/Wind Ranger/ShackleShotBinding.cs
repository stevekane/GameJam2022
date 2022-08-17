using UnityEngine;

public class ShackleShotBinding : MonoBehaviour {
  public LineRenderer LineRenderer;
  public ShackleShotEffect FirstEffect;
  public ShackleShotEffect SecondEffect;
  public Status First;
  public Status Second;
  public float YOffset;

  void Start() {
    FirstEffect = new ShackleShotEffect();
    SecondEffect = new ShackleShotEffect();
    First.Add(FirstEffect);
    Second.Add(SecondEffect);
  }
  void Stop() {
    First.Remove(FirstEffect);
    Second.Remove(SecondEffect);
    Destroy(gameObject);
  }
  void OnDestroy() {
    Stop();
  }
  void FixedUpdate() {
    if (!First.Active.Contains(FirstEffect) || !Second.Active.Contains(SecondEffect)) {
      Stop();
    }
  }
  void LateUpdate() {
    LineRenderer.SetPosition(0, First.transform.position+Vector3.up*YOffset);
    LineRenderer.SetPosition(1, Second.transform.position+Vector3.up*YOffset);
  }
}