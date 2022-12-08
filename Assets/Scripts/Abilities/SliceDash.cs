using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceDash : Ability {
  public float MoveSpeed = 100f;
  public Timeval WindupDuration = Timeval.FromAnimFrames(2, 30);
  public Timeval Duration = Timeval.FromSeconds(.3f);
  public AnimationClip WindupClip;
  public AnimationClip DashingClip;
  public AnimationClip DoneClip;
  public AttackHitbox Hitbox;
  public HitConfig HitConfig;
  List<Hurtbox> Hits = new();
  Animator Animator;

  public IEnumerator Execute() {
    var dir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    if (dir == Vector3.zero)
      dir = AbilityManager.transform.forward.XZ();
    AddStatusEffect(new ScriptedMovementEffect());
    yield return Animator.Run(WindupClip);
    AddStatusEffect(new InlineEffect(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }));
    var countdown = new CountdownTimer(Duration);
    yield return Fiber.Any(Animator.Run(DashingClip), countdown, Move(dir.normalized, countdown));
    foreach (var h in Hits)
      h.TryAttack(Attributes, HitConfig);
    yield return Animator.Run(DoneClip);
  }

  public override void OnStop() {
    Hitbox.Collider.enabled = false;
    Hitbox.TriggerEnter = null;
  }

  public IEnumerator Move(Vector3 dir, CountdownTimer timer) {
    Hits.Clear();
    Hitbox.Collider.enabled = true;
    Hitbox.TriggerEnter = OnContact;
    while (true) {
      Status.transform.forward = dir;
      var move = MoveSpeed * Time.fixedDeltaTime * dir;
      var front = Status.transform.position + Status.transform.forward.XZ()*5f;
      foreach (var h in Hits) {
        var delta = (front - h.transform.position).XZ();
        h.GetComponent<Status>().Move(move + (1f / timer.Value.Ticks) * delta);
      }
      Status.Move(move);
      yield return null;
    }
  }
  public void OnContact(Hurtbox hurtbox) {
    if (!Hits.Contains(hurtbox) && hurtbox.Owner.TryGetComponent(out Status status)) {
      status.Add(Using(new ScriptedMovementEffect()));
      Hits.Add(hurtbox);
    }
  }

  public override void Awake() {
    base.Awake();
    Animator = GetComponentInParent<Animator>();
  }
}