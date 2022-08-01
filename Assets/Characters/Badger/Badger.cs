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
  AbilityUser Abilities;
  AbilityUser TargetAbilities;
  Shield Shield;
  int WaitFrames = 0;
  int RecoveryFrames = 0;
  Vector3 Velocity;
  AbilityFibered CurrentAbility;
  ShieldAbility ShieldAbility;

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    TargetAbilities = Target.GetComponent<AbilityUser>();
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Animator = GetComponent<Animator>();
    Defender = GetComponent<Defender>();
    Shield = GetComponentInChildren<Shield>();
    Abilities = GetComponent<AbilityUser>();
    ShieldAbility = GetComponentInChildren<ShieldAbility>();
  }

  // TODO: What about dash?
  bool TargetIsAttacking { get => TargetAbilities.Abilities.Any((a) => !a.IsComplete); }

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
        //CurrentAbility = Abilities.TryStartAbilityF(1);
        WaitFrames = Timeval.FromMillis(1000).Frames;
      } else if (inRange) {
        CurrentAbility = Abilities.TryStartAbilityF(0);
        WaitFrames = AttackDelay.Frames;
      }
    }
    if (CurrentAbility == ShieldAbility) {
      if (!Shield) {
        ShieldAbility.Release();
      } else {
        if (TargetIsAttacking)
          WaitFrames = Timeval.FromMillis(1000).Frames;
        if (WaitFrames <= 0) {
          ShieldAbility.Release();
        }
      }
    }

    if (!inRange && Status.CanMove && CurrentAbility == null && WaitFrames <= 0 && RecoveryFrames <= 0) {
      transform.forward = desiredFacing;
      var dir = (desiredPos - transform.position).normalized;
      Velocity.SetXZ(MoveSpeed * dir);
      var gravity = -200f * Time.fixedDeltaTime;
      Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;
      Controller.Move(Velocity*Time.fixedDeltaTime);
    }

    if ((CurrentAbility == null || Defender.IsBlocking) && WaitFrames > 0)
      --WaitFrames;
    if (Status.IsHitstun) {
      RecoveryFrames = Timeval.FromMillis(1000).Frames;
      ShieldAbility.End();
    } else if (RecoveryFrames > 0) {
      --RecoveryFrames;
    }

    Animator.SetBool("HitFlinch", Status.IsHitstun);
    Defender.IsBlocking = ShieldAbility.IsRaised;
    if (Shield)
      Shield.Defender.IsBlocking = !Defender.IsBlocking;
  }
}
