using System.Linq;
using UnityEngine;

public class Badger : MonoBehaviour {
  public float MoveSpeed = 15f;
  public float AttackRange = 2f;
  public Timeval AttackDelay = Timeval.FromMillis(1000);
  CharacterController Controller;
  Status Status;
  Animator Animator;
  Defender Defender;
  Transform Target;
  AbilityManager Abilities;
  AbilityManager TargetAbilities;
  Shield Shield;
  int WaitFrames = 0;
  int RecoveryFrames = 0;
  Vector3 Velocity;
  Ability CurrentAbility;
  MeleeAttackAbility PunchAbility;
  ShieldAbility ShieldAbility;

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    TargetAbilities = Target.GetComponent<AbilityManager>();
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Animator = GetComponent<Animator>();
    Defender = GetComponent<Defender>();
    Shield = GetComponentInChildren<Shield>();
    Abilities = GetComponent<AbilityManager>();
    PunchAbility = GetComponentInChildren<MeleeAttackAbility>();
    ShieldAbility = GetComponentInChildren<ShieldAbility>();
  }

  // TODO: What about dash?
  bool TargetIsAttacking { get => TargetAbilities.Abilities.Any((a) => a.IsRunning); }

  Vector3 ChoosePosition() {
    var t = Target.transform;
    var d = AttackRange*.9f;
    var choices = new[] { t.position + t.right*d, t.position - t.right*d, t.position - t.forward*d };
    var closest = choices[0];
    foreach (var p in choices) {
      if ((p - transform.position).XZ().sqrMagnitude < (p - closest).XZ().sqrMagnitude)
        closest = p;
    }
    return closest;
  }
  bool IsInRange(Vector3 pos) {
    var delta = (pos - transform.position).XZ();
    return delta.sqrMagnitude < .1f;
  }

  void FixedUpdate() {
    if (Target == null)
      return;

    if (!CurrentAbility?.IsRunning ?? false)
      CurrentAbility = null;

    var desiredPos = ChoosePosition();
    var desiredFacing = (Target.position - transform.position).XZ().normalized;
    var inRange = IsInRange(desiredPos);

    if (Shield && !Shield.isActiveAndEnabled)
      Shield = null;

    if (Status.CanAttack && CurrentAbility == null && RecoveryFrames <= 0) {
      if (TargetIsAttacking && Shield) {
        Abilities.GetEvent(ShieldAbility.HoldStart).Fire();
        CurrentAbility = Abilities.Abilities.FirstOrDefault((a) => a.IsRunning);
        WaitFrames = Timeval.FromMillis(1000).Frames;
      } else if (inRange) {
        Abilities.GetEvent(PunchAbility.AttackStart).Fire();
        CurrentAbility = Abilities.Abilities.FirstOrDefault((a) => a.IsRunning);
        WaitFrames = AttackDelay.Frames;
      }
    }
    if (CurrentAbility == ShieldAbility) {
      if (!Shield) {
        Abilities.GetEvent(ShieldAbility.HoldRelease).Fire();
      } else {
        if (TargetIsAttacking)
          WaitFrames = Timeval.FromMillis(1000).Frames;
        if (WaitFrames <= 0) {
          Abilities.GetEvent(ShieldAbility.HoldRelease).Fire();
        }
      }
    }

    if (!inRange && Status.CanMove && CurrentAbility == null && WaitFrames <= 0 && RecoveryFrames <= 0) {
      transform.forward = desiredFacing;
      var dir = (desiredPos - transform.position).normalized;
      Velocity.SetXZ(MoveSpeed * Status.MoveSpeedFactor * dir);
      var gravity = -200f * Time.fixedDeltaTime;
      Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;
      if (!Status.HasGravity)
        Velocity.y = 0f;
      Controller.Move(Time.fixedDeltaTime * Velocity);
    }

    if ((CurrentAbility == null || Defender.IsBlocking) && WaitFrames > 0)
      --WaitFrames;
    if (Status.Get<KnockbackEffect>() != null) {
      RecoveryFrames = Timeval.FromMillis(1000).Frames;
      ShieldAbility.Stop();
    } else if (RecoveryFrames > 0) {
      --RecoveryFrames;
    }

    Defender.IsBlocking = ShieldAbility.IsRaised;
    if (Shield)
      Shield.Defender.IsBlocking = !Defender.IsBlocking;
  }
}
