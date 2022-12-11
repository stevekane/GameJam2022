using System.Collections;
using UnityEngine;

public class HybridAnimationMan : MonoBehaviour {
  [SerializeField] Status Status;
  [SerializeField] Timeval Duration = Timeval.FromMillis(500);

  void Awake() => InputManager.Instance.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(StunSelf);
  void StunSelf() => StartCoroutine(StunSelfRoutine());

  IEnumerator StunSelfRoutine() {
    Debug.Log("Stunning self");
    var effect = new InlineEffect(s => {
      s.IsHurt = false;
    });
    Status.Add(effect);
    for (var i = 0; i < Duration.Ticks; i++) {
      yield return new WaitForFixedUpdate();
    }
    Status.Remove(effect);
  }
}