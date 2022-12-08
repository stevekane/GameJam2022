using System.Collections;
using UnityEngine;

public class Shield : MonoBehaviour {
  public float MaxDamage = 50f;
  public Animator Animator;
  public AnimationClip DeathClip;
  public Hurtbox Hurtbox;
  Damage Damage;
  Bundle Bundle = new();

  IEnumerator WatchDamage() {
    yield return Fiber.While(() => Damage.Points < MaxDamage);
    Destroy(Hurtbox.gameObject);
    yield return new AnimationTask(Animator, DeathClip, true);
    Destroy(gameObject, .01f);
  }

  void Awake() {
    Damage = GetComponent<Damage>();
    Bundle.StartRoutine(WatchDamage());
  }

  void FixedUpdate() => Bundle.MoveNext();
}