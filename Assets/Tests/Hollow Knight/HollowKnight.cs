using System;
using UnityEngine;
using UnityEngine.UI;
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
  public float BladeHeight = .5f;
  public float JumpStrength = 10;
  public float MoveSpeed = 10;
  public float MoveAcceleration = 10;
  public float SkinThickness = .05f;
  public TextMeshProUGUI ActionText;

  [Header("State")]
  public Vector3 Velocity;
  public bool FacingLeft = false;
  public Condition Condition = new();
  public ConditionAccum ConditionAccum = new();
  public Bundle Bundle;

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
    if (Controller.Collisions.Bottom) {
      Velocity.y = JumpStrength;
    }
  }

  void OnWest() {
    if (Condition.CanAttack) {
      var x = InputManager.AxisLeft.XY.x;
      var y = InputManager.AxisLeft.XY.y;
      var useY = Mathf.Abs(x) > Mathf.Abs(y);
      var offset = Vector2.left;

      if (x == 0 && y == 0) {
        offset = BladeHeight*Vector2.up + (FacingLeft ? Vector2.left : Vector2.right);
      } else if (Mathf.Abs(x) > Mathf.Abs(y)) {
        offset = BladeHeight*Vector2.up + (x < 0 ? Vector2.left : Vector2.right);
      } else {
        offset = y > 0 ? Vector2.up*1.5f : Vector2.down*.75f;
      }
      Bundle.Run(new HollowKnightMeleeAttack(
        transform,
        ConditionAccum,
        SawPrefab,
        offset,
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

    if (Condition.CanMove && Math.Abs(stick.x) > 0) {
      Velocity.x = Mathf.Sign(stick.x)*MoveSpeed;
    } else {
      Velocity.x = 0;
    }

    if (Controller.Collisions.Bottom && Velocity.y <= 0) {
      Velocity.y = Physics2D.gravity.y*Time.fixedDeltaTime;
    } else {
      Velocity.y = Velocity.y+Physics2D.gravity.y*Time.fixedDeltaTime;
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