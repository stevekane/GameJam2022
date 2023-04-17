using UnityEngine;

public class MeleeAttacker : MonoBehaviour {
  [Header("Effects")]
  [SerializeField] GameObject OnHitVFX;

  [Header("Components")]
  [SerializeField] Animator Animator;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] MeleeAttackTargeting MeleeAttackTargeting;

  [Header("State")]
  [SerializeField] HitStop HitStop;

  void OnHit(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks;
  }

  void OnBlocked(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2;
  }

  // TODO: Restore the idea of cancelling attack ability if parried
  void OnParried(MeleeContact contact) {
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHurt(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2;
    Animator.SetTrigger("Parried");
  }
}