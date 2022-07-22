using System.Collections.Generic;
using UnityEngine;

public class AbilityMan : MonoBehaviour {
  interface IAbility {
    public bool IsComplete { get; }
    public void Start();
    public void Stop();
    public void Step();
    public bool TryStart();
    public bool TryStop();
    public bool CanStart();
    public bool CanStop();
    public void OnStart();
    public void OnStop();
    public void OnStep();
  }

  abstract class Ability : IAbility {
    public bool IsComplete { get; private set; }
    public void Start() {
      OnStart();
      IsComplete = false;
    }
    public void Stop() {
      OnStop();
      IsComplete = true;
    }
    public void Step() {
      OnStep();
    }
    public bool TryStart() {
      if (CanStart()) {
        Start();
        return true;
      } else {
        return false;
      }
    }
    public bool TryStop() {
      if (CanStop()) {
        Stop();
        return true;
      } else {
        return false;
      }
    }
    public virtual void OnStart() {}
    public virtual void OnStop() {}
    public virtual void OnStep() {}
    public virtual bool CanStart() => true;
    public virtual bool CanStop() => true;
  }

  delegate void Continuation();

  abstract class AbilityTask {
    public Continuation Continuation;
    public bool IsComplete { get; private set; }
    public void Start() {
      OnStart();
      IsComplete = false;
    }
    public void Stop() {
      OnStop();
      IsComplete = true;
    }
    public void Step() {
      OnStep();
    }
    public virtual void OnStart() {}
    public virtual void OnStop() {}
    public virtual void OnStep() {}

    #region FLUENT_API
    public AbilityTask Activate(AbilityGraph graph) {
      graph.StartTask(this);
      return this;
    }
    public AbilityTask AddOutput(Continuation output) {
      Continuation += output;
      return this;
    }
    public AbilityTask RemoveOutput(Continuation output) {
      Continuation += output;
      return this;
    }
    #endregion
  }

  abstract class AbilityGraph : IAbility {
    public bool IsComplete { get; private set; }
    public List<AbilityTask> Tasks = new List<AbilityTask>();

    public void Start() {
      OnStart();
      IsComplete = false;
    }

    public void Stop() {
      OnStop();
      Tasks.ForEach(t => t.Stop());
      Tasks.Clear();
      IsComplete = true;
    }

    public void Step() {
      OnStep();
      RunTasks();
      if (Tasks.Count <= 0) {
        Stop();
      }
    }
    public bool TryStart() {
      if (CanStart()) {
        Start();
        return true;
      } else {
        return false;
      }
    }
    public bool TryStop() {
      if (CanStop()) {
        Stop();
        return true;
      } else {
        return false;
      }
    }
    public virtual void OnStart() {}
    public virtual void OnStop() {}
    public virtual void OnStep() {}
    public virtual bool CanStart() => true;
    public virtual bool CanStop() => true;

    public void StartTask(AbilityTask task) {
      task.Start();
      Tasks.Add(task);
    }

    void RunTasks() {
      // backward iteration since tasks array may be mutated
      for (var i = Tasks.Count-1; i >= 0; i--) {
        var t = Tasks[i];
        t.OnStep();
        if (t.IsComplete) {
          Tasks.RemoveAt(i);
        }
      }
    }
  }

  class ShoutKey : Ability {
    public override void OnStart() {
      Debug.Log("Begin shouting");
    }
    public override void OnStop() {
      Debug.Log("End shouting");
    }
    public override void OnStep() {
      var action = Inputs.Action;
      var dt = Time.fixedDeltaTime;
      if (action.L1.JustDown) {
        Debug.Log("L1");
      } else if (action.L2.JustUp) {
        Stop();
      }
    }
  }

  class FartAbilityTask : AbilityTask {
    public override void OnStep() {
      Debug.Log("Fart");
      Stop();
      Continuation?.Invoke();
    }
  }

  class BurpAbilityTask : AbilityTask {
    public override void OnStep() {
      Debug.Log("Burp");
      Stop();
      Continuation?.Invoke();
    }
  }

  class WaitAbilityTask : AbilityTask {
    public int Frames;
    public override void OnStart() {
      Debug.Log($"Waitin {Frames} frames...");
    }
    public override void OnStep() {
      if (Frames <= 0) {
        Stop();
        Continuation.Invoke();
      } else {
        Frames--;
      }
    }
  }

  class FartWaitBurpAbility : AbilityGraph {
    public override void OnStart() {
      new FartAbilityTask()
      .Activate(this)
      .AddOutput(delegate {
        new WaitAbilityTask { Frames = 1000 }
        .Activate(this)
        .AddOutput(delegate { new BurpAbilityTask().Activate(this); })
        .AddOutput(delegate { new BurpAbilityTask().Activate(this); });
      });
    }
  }

  class SetAnimatorParam : AbilityTask {
    public abstract class ParamType {
      public string Key;
      public abstract void Set(Animator animator);
    }

    public class FloatParam : ParamType {
      public float Value;
      public FloatParam(string name, float value) {
        Key = name;
        Value = value;
      }
      public override void Set(Animator animator) {
        animator.SetFloat(Key, Value);
      }
    }

    public class IntParam : ParamType {
      public int Value;
      public IntParam(string name, int value) {
        Key = name;
        Value = value;
      }
      public override void Set(Animator animator) {
        animator.SetInteger(Key, Value);
      }
    }

    public class BoolParam : ParamType {
      public bool Value;
      public BoolParam(string name, bool value) {
        Key = name;
        Value = value;
      }
      public override void Set(Animator animator) {
        animator.SetBool(Key, Value);
      }
    }

    public class TriggerParam : ParamType {
      public TriggerParam(string name) {
        Key = name;
      }
      public override void Set(Animator animator) {
        animator.SetTrigger(Key);
      }
    }

    Animator Animator;
    ParamType Param;

    SetAnimatorParam(Animator animator, ParamType param) {
      Animator = animator;
      Param = param;
    }

    public override void OnStart() {
      Param.Set(Animator);
    }
  }

  // TODO: Maybe you could break this into Aim + Wait and a combinator Either
  // then you would say Either(Aim,Wait) -> Next
  class Aim : AbilityTask {
    public float TurnSpeed;
    public int Frames;
    public Transform Owner;
    public Transform Target;
    public override void OnStep() {
      if (Frames > 0) {
        var current = Owner.forward;
        var desired = Target.position-Owner.position;
        Owner.forward = Vector3.RotateTowards(current, desired, Time.fixedDeltaTime*TurnSpeed, 0);
        Frames--;
      } else {
        Stop();
        Continuation?.Invoke();
      }
    }
  }

  class TripleShot : AbilityGraph {
    public Timeval AimDuration;
    public Timeval ShotCooldown;
    public Transform Owner;
    public Transform Target;
    public Transform Origin;
    public Rigidbody ProjectilePrefab;
    public AudioClip FireSound;
    public Animator Animator;
    public AudioSource AudioSource;
    public override void OnStart() {
      Animator.SetBool("Attacking", true);
      new Aim { Frames = AimDuration.Frames, TurnSpeed = Mathf.PI, Target = Target, Owner = Owner }
      .Activate(this)
      .AddOutput(delegate {
        Fire();
        new WaitAbilityTask { Frames = ShotCooldown.Frames }
        .Activate(this)
        .AddOutput(delegate {
          Fire();
          new WaitAbilityTask { Frames = ShotCooldown.Frames }
          .Activate(this)
          .AddOutput(delegate {
            Fire();
          });
        });
      });
    }
    public override void OnStop() {
      Animator.SetBool("Attacking", false);
    }

    void Fire() {
      Debug.Log("Fire");
      Animator.SetTrigger("Attack");
      AudioSource.PlayOptionalOneShot(FireSound);
      Instantiate(ProjectilePrefab, Origin.transform.position, Owner.transform.rotation)
      .AddForce(Owner.transform.forward*100, ForceMode.Impulse);
    }
  }

  [SerializeField] float MOVE_SPEED = 10;
  [SerializeField] CharacterController Controller;
  [SerializeField] Transform Target;
  [SerializeField] Transform Muzzle;
  [SerializeField] Animator Animator;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Rigidbody ProjectilePrefab;
  [SerializeField] AudioClip FireSound;
  IAbility[] Abilities;
  IAbility CurrentAbility;

  bool TryStartAbility(IAbility ability) {
    if (ability.TryStart()) {
      CurrentAbility = ability;
      return true;
    } else {
      return false;
    }
  }

  void Start() {
    Abilities = new IAbility[3] {
      new ShoutKey(),
      new FartWaitBurpAbility(),
      new TripleShot {
        AimDuration = Timeval.FromTicks(1000, 1000),
        ShotCooldown = Timeval.FromTicks(500, 1000),
        Target = Target,
        Origin = Muzzle,
        Owner = transform,
        ProjectilePrefab = ProjectilePrefab,
        FireSound = FireSound,
        Animator = Animator,
        AudioSource = AudioSource
      }
    };
  }

  void FixedUpdate() {
    var action = Inputs.Action;
    var dt = Time.fixedDeltaTime;
    var move = action.Left.XZ;

    if (CurrentAbility != null && CurrentAbility.IsComplete) {
      CurrentAbility = null;
    }
    if (CurrentAbility == null) {
      if (action.L2.JustDown) {
        TryStartAbility(Abilities[0]);
      } else if (action.R1.JustDown) {
        TryStartAbility(Abilities[1]);
      } else if (action.R2.JustDown) {
        TryStartAbility(Abilities[2]);
      }
    }

    if (CurrentAbility == null && move.magnitude > 0) {
      transform.forward = move;
    }
    Controller.Move(dt*MOVE_SPEED*Inputs.Action.Left.XZ);
    CurrentAbility?.Step();
  }
}