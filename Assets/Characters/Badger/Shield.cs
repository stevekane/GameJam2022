using System.Collections;
using UnityEngine;

public class Shield : MonoBehaviour {
  public float MaxDamage = 50f;
  public Animator Animator;
  public AnimationClip DeathClip;
  Damage Damage;
  Bundle Bundle = new();
  public Defender Defender { get; private set; }

  IEnumerator WatchDamage() {
    yield return Fiber.While(() => Damage.Points < MaxDamage);
    yield return new AnimationTask(Animator, DeathClip, true);
    Destroy(gameObject, .01f);
  }

  void Awake() {
    Damage = GetComponent<Damage>();
    Defender = GetComponent<Defender>();
    Bundle.StartRoutine(WatchDamage());
  }

  void FixedUpdate() => Bundle.MoveNext();
}
