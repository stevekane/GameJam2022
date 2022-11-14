using System;
using UnityEngine;

[Serializable]
public class Condition {
  public bool CanMove;
  public bool CanTurn;
  public bool CanAttack;
  public void Reset() => CanMove = CanTurn = CanAttack = true;
}

[Serializable]
public class ConditionAccum {
   public int CanMoveCount { get; internal set; }
   public int CanTurnCount { get; internal set; }
   public int CanAttackCount { get; internal set; }
   public bool CanMove { set => CanMoveCount += value ? 1 : -1; }
   public bool CanTurn { set => CanTurnCount += value ? 1 : -1; }
   public bool CanAttack { set => CanAttackCount += value ? 1 : -1; }
   public void Reset() => CanMoveCount = CanTurnCount = CanAttackCount = 0;
}

public class HollowKnight : MonoBehaviour {
  [Header("Config")]
  public Animator SawPrefab;
  public float BladeHeight = .5f;
  public float JumpStrength = 10;
  public float MoveSpeed = 10;
  public float MoveAcceleration = 10;
  public float SkinThickness = .05f;

  [Header("State")]
  public Vector3 Velocity;
  public bool FacingLeft = false;
  public Condition Condition = new();
  public ConditionAccum ConditionAccum = new();
  public Bundle Bundle;

  InputManager InputManager;
  CharacterController2D Controller;
  SpriteRenderer SpriteRenderer;
  Animator Animator;

  void Awake() {
    InputManager = FindObjectOfType<InputManager>();
    SpriteRenderer = GetComponent<SpriteRenderer>();
    Controller = GetComponent<CharacterController2D>();
    Animator = GetComponent<Animator>();
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(OnSouth);
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(OnWest);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(OnSouth);
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(OnWest);
  }

  void OnSouth() {
    if (Controller.Collisions.Bottom) {
      Velocity.y = JumpStrength;
    }
  }

  void OnWest() {
    if (Condition.CanAttack) {
      Bundle.Run(new HollowKnightMeleeAttack(
        transform,
        ConditionAccum,
        SawPrefab,
        BladeHeight*Vector2.up + (FacingLeft ? Vector2.left : Vector2.right),
        Timeval.FromMillis(500)));
    }
  }

  void FixedUpdate() {
    var stick = InputManager.AxisLeft.XY;

    if (Controller.Collisions.Bottom && Velocity.y <= 0) {
      Velocity.y = 0;
    }
    Velocity.x = Mathf.Abs(stick.x) > 0 ? Mathf.Sign(stick.x)*MoveSpeed : 0;
    Velocity.y = Velocity.y+Physics2D.gravity.y*Time.fixedDeltaTime;
    Controller.Move(Velocity*Time.fixedDeltaTime);

    if (Condition.CanTurn) {
      FacingLeft = stick.x switch {
        < 0 => true,
        > 0 => false,
        _ => FacingLeft
      };
      SpriteRenderer.flipX = FacingLeft;
    }

    if (Controller.Collisions.Bottom) {
      if (stick.x == 0) {
        Animator.Play("HollowKnight Idle Animation");
      } else {
        Animator.Play("HollowKnight Run Animation");
      }
    } else {
      Animator.Play("HollowKnight Jump Animation");
    }

    // Process running abilities
    Bundle.MoveNext();
    // Process condition accumulators into actual condition
    {
      Condition.CanMove = ConditionAccum.CanMoveCount >= 0;
      Condition.CanTurn = ConditionAccum.CanTurnCount >= 0;
      Condition.CanAttack = ConditionAccum.CanAttackCount >= 0;
      ConditionAccum.Reset();
    }
  }
}
