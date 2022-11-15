using System;
using UnityEngine;
using TMPro;

[Serializable]
public class PropertyBool {
  bool Base = true;
  int Count = 0;
  public bool Current => Count >= 0;
  public PropertyBool(bool b) => Base = b;
  public void Set(bool b) => Count += b ? 1 : -1;
  public void Reset() => Count = 0;
}

[Serializable]
public class PropertyFloat {
  [SerializeField] float Base;
  float Added;
  float Multiplied;
  public float Current => (Base+Added)*Multiplied;
  public PropertyFloat(float b) => Base = b;
  public void Add(float v) => Added+=v;
  public void Mul(float v) => Multiplied+=v;
  public void Reset() {
    Added = 0;
    Multiplied = 1;
  }
}

[Serializable]
public class Condition {
  public bool CanMove;
  public bool CanJump;
  public bool CanAct;
  public float MoveSpeed;
  public float JumpSpeed;
  public void Set(ConditionAccum accum) {
    CanMove = accum.CanMove.Current;
    CanJump = accum.CanJump.Current;
    CanAct = accum.CanAct.Current;
    MoveSpeed = accum.MoveSpeed.Current;
    JumpSpeed = accum.JumpSpeed.Current;
  }
}

[Serializable]
public class ConditionAccum {
  public PropertyBool CanMove = new(true);
  public PropertyBool CanJump = new(true);
  public PropertyBool CanAct = new(true);
  public PropertyFloat MoveSpeed = new(10);
  public PropertyFloat JumpSpeed = new(10);
  public void Reset() {
    CanMove.Reset();
    CanJump.Reset();
    CanAct.Reset();
    MoveSpeed.Reset();
    JumpSpeed.Reset();
  }
}

public class HollowKnight : MonoBehaviour {
  [Header("Config")]
  public Animator SawPrefab;
  public Timeval CoyoteDuration = Timeval.FromMillis(100);
  public Timeval JumpBufferDuration = Timeval.FromMillis(100);
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
    if (Condition.CanJump) {
      JumpRequested = true;
    }
  }

  void OnWest() {
    if (Condition.CanAct) {
      var x = InputManager.AxisLeft.XY.x;
      var y = InputManager.AxisLeft.XY.y;
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
        destination));
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

    Velocity.x =
      Condition.CanMove
        ? Math.Abs(stick.x) > 0
          ? Mathf.Sign(stick.x)*Condition.MoveSpeed
          : 0
        : Velocity.x;
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
      Velocity.y = Condition.JumpSpeed;
      JumpBufferRemaining = 0;
      CoyoteFramesRemaining = 0;
    }

    Controller.Move(Velocity*Time.fixedDeltaTime);

    if (Condition.CanMove) {
      FacingLeft = stick.x switch {
        < 0 => true,
        > 0 => false,
        _ => FacingLeft
      };
      SpriteRenderer.flipX = FacingLeft;
    }

    if (Controller.Collisions.Bottom) {
      if (Velocity.x == 0) {
        Animator.Play("Idle");
      } else {
        Animator.Play("Run");
      }
    } else {
      if (Velocity.y <= 0) {
        Animator.Play("Fall");
      } else {
        Animator.Play("Jump");
      }
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
    Condition.Set(ConditionAccum);
    ConditionAccum.Reset();
  }
}