using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobShitter : Mob {
  public override void TakeDamage() {
    StartCoroutine(DestroySequence());
  }

  IEnumerator DestroySequence() {
    const float duration = .5f;
    float timer = 0f;
    Vector3 scale = transform.localScale;
    while (timer < duration) {
      transform.localScale = scale * (1 - timer/duration);
      yield return null;
      timer += Time.deltaTime;
    }
    Destroy(gameObject);
  }
}
