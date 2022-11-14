using System;
using UnityEngine;
using TMPro;

[Serializable]
public class Condition {
  public bool CanMove;
  public bool CanTurn;
  public bool CanAttack;
  public bool CanAct;
  public void Reset() => CanMove = CanTurn = CanAttack = CanAct = true;
}

[Serializable]
public class ConditionAccum {
   public int CanMoveCount { get; internal set; }
   public int CanTurnCount { get; internal set; }
   public int CanAttackCount { get; internal set; }
   public int CanActCount { get; internal set; }
   public bool CanMove { set => CanMoveCount += value ? 1 : -1; }
   public bool CanTurn { set => CanTurnCount += value ? 1 : -1; }
   public bool CanAttack { set => CanAttackCount += value ? 1 : -1; }
   public bool CanAct { set => CanActCount += value ? 1 : -1; }
   public void Reset() => CanMoveCount = CanTurnCount = CanAttackCount = CanActCount = 0;
}

public class HollowKnight : MonoBehaviour {
  [Header("Config")]
  public Animator SawPrefab;
  public Timeval BladeDuration = Timeval.FromMillis(250);
  public Timeval CoyoteDuration = Timeval.FromMillis(100);
  public Timeval JumpBufferDuration = Timeval.FromMillis(100);
  public float BladeHeight = .5f;
  public float JumpStrength = 10;
  public float MoveSpeed = 10;
  public float MoveAcceleration = 10;
  public float SkinThickness = .05f;
  public TextMeshProUGUI ActionText;
  public Transform Center;

  [Header("State")]
  public Vector3 Velocity;
  public bool FacingLeft = false;
  public bool JumpRequested = false;
  public Condition Condition = new();
  public ConditionAccum ConditionAccum = new();
  public Bundle Bundle;
  public int CoyoteFramesRemaining;
  public int JumpBufferRemaining;

  CharacterController2D Controller;
  SpriteRenderer SpriteRenderer;
  InputManager InputManager;
  Interactor Interactor;
  Animator Animator;

  void Awake() {
    Controller = GetComponent<CharacterController2D>();
    SpriteRenderer = GetComponent<SpriteRenderer>();
    InputManager = FindObjectOfType<InputManager>();
    Interactor = FindObjectOfType<Interactor>();
    Animator = GetComponent<Animator>();
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(OnSouth);
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(OnWest);
    InputManager.ButtonEvent(ButtonCode.L2, ButtonPressType.JustDown).Listen(OnAct);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(OnSouth);
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(OnWest);
    InputManager.ButtonEvent(ButtonCode.L2, ButtonPressType.JustDown).Unlisten(OnAct);
    Bundle.Stop();
  }

  void OnSouth() {
    if (Condition.CanAct) {
      JumpRequested = true;
    }
  }

  void OnWest() {
    if (Condition.CanAttack) {
      var x = InputManager.AxisLeft.XY.x;
      var y = InputManager.AxisLeft.XY.y;
      var useY = Mathf.Abs(x) > Mathf.Abs(y);
      var destination = Vector2.zero;

      if (x == 0 && y == 0) {
        destination = FacingLeft ? Vector2.left : Vector2.right;
      } else if (Mathf.Abs(x) > Mathf.Abs(y)) {
        destination = x < 0 ? Vector2.left : Vector2.right;
      } else {
        destination = y > 0 ? Vector2.up : Vector2.down;
      }
      Bundle.Run(new HollowKnightMeleeAttack(
        Center,
        ConditionAccum,
        SawPrefab,
        destination,
        BladeDuration));
    }
  }

  void OnAct() {
    if (Condition.CanAct && TryGetComponent(out Interactor i)) {
      i.Target?.Interact(ConditionAccum, InputManager.ButtonEvent(ButtonCode.L2, ButtonPressType.JustDown));
    }
  }

  void FixedUpdate() {
    var stick = InputManager.AxisLeft.XY;
    var grounded = Controller.Collisions.Bottom && Velocity.y <= 0;

    Velocity.x = Condition.CanMove && Math.Abs(stick.x) > 0
      ? Mathf.Sign(stick.x)*MoveSpeed
      : 0;
    Velocity.y = grounded
      ? Physics2D.gravity.y*Time.fixedDeltaTime
      : Velocity.y+Physics2D.gravity.y*Time.fixedDeltaTime;
    CoyoteFramesRemaining = grounded
      ? CoyoteDuration.Frames
      : CoyoteFramesRemaining-1;
    JumpBufferRemaining = JumpRequested
      ? JumpBufferDuration.Frames
      : JumpBufferRemaining-1;
    JumpRequested = false;

    if (CoyoteFramesRemaining > 0 && JumpBufferRemaining > 0) {
      Velocity.y = JumpStrength;
      JumpBufferRemaining = 0;
      CoyoteFramesRemaining = 0;
    }

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
      if (Velocity.x == 0) {
        Animator.Play("HollowKnight Idle Animation");
      } else {
        Animator.Play("HollowKnight Run Animation");
      }
    } else {
      Animator.Play("HollowKnight Jump Animation");
    }

    if (Interactor.Target) {
      ActionText.gameObject.SetActive(true);
      if (Condition.CanAct) {
        ActionText.text = Interactor.Target.InteractMessage + " (L2)";
      } else {
        ActionText.text = Interactor.Target.StopMessage + " (L2)";
      }
    } else {
      ActionText.gameObject.SetActive(false);
    }

    Bundle.MoveNext();
    {
      Condition.CanMove = ConditionAccum.CanMoveCount >= 0;
      Condition.CanTurn = ConditionAccum.CanTurnCount >= 0;
      Condition.CanAttack = ConditionAccum.CanAttackCount >= 0;
      Condition.CanAct = ConditionAccum.CanActCount >= 0;
      ConditionAccum.Reset();
    }
  }
}