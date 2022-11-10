using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SliceDash : Ability {
  public float MoveSpeed = 100f;
  public Timeval WindupDuration = Timeval.FromTicks(2, 30);
  public Timeval Duration = Timeval.FromSeconds(.3f);
  public AnimationClip WindupClip;
  public AnimationClip DashingClip;
  public AnimationClip DoneClip;
  public AttackHitbox Hitbox;
  public HitConfig HitConfig;
  List<Defender> Hits = new();
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
      h.OnHit(HitConfig.ComputeParams(Attributes), AbilityManager.transform);
    //yield return Animator.Run(DoneClip);
  }

  public override void Stop() {
    base.Stop();
    Hitbox.Collider.enabled = false;
    Hitbox.TriggerEnter = null;
    Bundle.StartRoutine(new Fiber(Animator.Run(DoneClip)));
  }

  public IEnumerator Move(Vector3 dir, CountdownTimer timer) {
    Hits.Clear();
    Hitbox.Collider.enabled = true;
    Hitbox.TriggerEnter = OnContact;
    while (true) {
      var move = MoveSpeed * Time.fixedDeltaTime * dir;
      var front = Status.transform.position + Status.transform.forward.XZ()*5f;
      foreach (var h in Hits) {
        var delta = (front - h.transform.position).XZ();
        h.GetComponent<Status>().Move(move + (1f / timer.Value.Frames) * delta);
      }
      Status.Move(move);
      yield return null;
    }
  }
  public void OnContact(Transform target) {
    if (target.TryGetComponent(out Defender d) && !Hits.Contains(d)) {
      d.GetComponent<Status>().Add(Using(new ScriptedMovementEffect()));
      Hits.Add(d);
    }
  }

  void Awake() {
    Animator = GetComponentInParent<Animator>();
  }
}
