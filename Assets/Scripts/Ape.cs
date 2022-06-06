using UnityEngine;

public enum ApeState { Dead, Falling, Jumping, Moving, Perching, Pouncing, Stomping }  
public enum ActionState { Windup, Active, Recovery }

public class Ape : MonoBehaviour {
  [Header("Configuration")]
  [SerializeField] ApeConfig Config;

  [Header("Components")]
  [SerializeField] CharacterController CharacterController;
  
  [Header("User Interface")]
  [SerializeField] Selector Selector;
  [SerializeField] Selector[] Selectors;

  [Header("State")]
  public ApeState State = ApeState.Moving;
  public ActionState ActionState = ActionState.Windup;
  public Vector3 ActionVelocity;
  public float ActionFramesRemaining;
  public float FallFramesRemaining;
  public float JumpFramesRemaining;
  public float AimingFramesRemaining;
  public Holdable Held;
  public Targetable Target;
  public Targetable PerchedOn;

  float Score(Vector3 forward, Vector3 origin, Vector3 target) {
    var delta = target - origin;
    var distance = delta.magnitude;
    var dot = distance > 0 ? Vector3.Dot(delta.normalized,forward) : 1;
    var a = Config.DistanceScore.Evaluate(1 - distance / Config.SearchRadius);
    var b = Config.AngleScore.Evaluate(Mathf.Lerp(0,1,Mathf.InverseLerp(-1,1,dot)));
    return a + b;
  }

  T FindClosest<T>(T ignore, MonoBehaviour[] components, Vector3 forward, Vector3 origin) where T : MonoBehaviour {
    T best = null;
    var bestScore = 0f;
    for (int i = 0; i < components.Length; i++) {
      var targetable = components[i].GetComponent<T>();
      var score = Score(forward,origin,components[i].transform.position);
      if (targetable && targetable != ignore && score > bestScore) {
        best = targetable;
        bestScore = score;
      }
    }
    return best;
  }

  void Select(MonoBehaviour[] components) {
    for (int i = 0; i < Selectors.Length; i++) {
      if (i < components.Length) {
        Selectors[i].Target = components[i].GetComponent<Targetable>();
        Selectors[i].gameObject.SetActive(true);
      } else {
        Selectors[i].Target = null;
        Selectors[i].gameObject.SetActive(false);
      }
    }
  }

  void ClearSelected() {
    for (int i = 0; i < Selectors.Length; i++) {
      Selectors[i].Target = null;
      Selectors[i].gameObject.SetActive(false);
      Selectors[i].transform.position = transform.position;
    }
  }

  void Highlight(MonoBehaviour component) {
    if (component) {
      Selector.Target = component.GetComponent<Targetable>();
      Selector.gameObject.SetActive(true);
    } else {
      Selector.Target = null;
      Selector.gameObject.SetActive(false);
    }
  }

  void ClearHighlighted() {
    Selector.Target = null;
    Selector.gameObject.SetActive(false);
    Selector.transform.position = transform.position;
  }

  void ScaleTime(float timeScale) {
    Time.timeScale = Inputs.InPlayBack ? 1 : timeScale;
  }

  void Glide(Vector3 magnitude, float dt) {
    CharacterController.Move(magnitude * dt);
  }

  void Move(Vector3 magnitude, float dt) {
    CharacterController.Move((magnitude + Physics.gravity) * dt);
  }

  void Drop(float dt) {
    CharacterController.Move(Physics.gravity * dt);
  }

  void LookAlong(Vector3 forward) {
    transform.rotation = Quaternion.LookRotation(forward,Vector3.up);
  }

  void Fall() {
    FallFramesRemaining = Config.FallFrames;
    State = ApeState.Falling;
  }

  void Jump(Vector3 direction) {
    var vector = transform.forward * Config.JumpDistance;
    var time = Config.JumpFrames * Time.fixedDeltaTime;
    ActionVelocity = vector / time;
    JumpFramesRemaining = Config.JumpFrames;
    State = ApeState.Jumping;
  }

  void Pounce(Targetable target) {
    var vector = target.transform.position - transform.position;
    var time = Config.PounceConfig.ActiveFrames * Time.fixedDeltaTime;
    ActionVelocity = vector / time;
    ActionState = ActionState.Windup;
    ActionFramesRemaining = Config.PounceConfig.WindupFrames;
    State = ApeState.Pouncing;
    Target = target;
  }

  void Stomp(Targetable target) {
    var vector = target.transform.position - transform.position;
    var time = Config.StompConfig.ActiveFrames * Time.fixedDeltaTime;
    ActionVelocity = vector / time;
    ActionState = ActionState.Windup;
    ActionFramesRemaining = Config.StompConfig.WindupFrames;
    State = ApeState.Stomping;
    Target = target;
  }

  void Perch(Targetable target) {
    PerchedOn = target;
    transform.SetParent(target.transform,true);
  }

  void Die() {
    ActionVelocity = Vector3.zero;
    FallFramesRemaining = 0;
    JumpFramesRemaining = 0;
    AimingFramesRemaining = 0;
    Held = null;
    Target = null;
    PerchedOn = null;
    State = ApeState.Dead;
  }

  bool Grabbable(Targetable targetable) {
    return Vector3.Distance(targetable.transform.position,transform.position) < Config.GrabRadius;
  }

  void Start() {
    State = ApeState.Moving;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var move = new Vector3(action.Move.x,0,action.Move.y);
    var aim = new Vector3(action.Aim.x,0,action.Aim.y);

    // Regeneration of Aiming Resource
    AimingFramesRemaining = aim.magnitude <= 0 
      ? Mathf.Min(AimingFramesRemaining+1,Config.AimingFrames)
      : AimingFramesRemaining-1;

    switch (State) {
      case ApeState.Falling: {
        if (CharacterController.isGrounded) {
          State = ApeState.Moving;
        } else if (FallFramesRemaining > 0) {
          FallFramesRemaining--; 
          Drop(dt);
        } else {
          Die();
        }
      }
      break;

      case ApeState.Jumping: {
        if (JumpFramesRemaining > 0) {
          JumpFramesRemaining--;
          Glide(ActionVelocity,dt);
        } else if (CharacterController.isGrounded) {
          State = ApeState.Moving;
        } else {
          Fall();
        }
      }
      break;

      case ApeState.Moving: {
        if (!CharacterController.isGrounded) {
          ClearSelected();
          ClearHighlighted();
          ScaleTime(1);
          Drop(dt);
          Fall();
        } else if (aim.magnitude > 0) {
          var targetables = FindObjectsOfType<Targetable>(false);
          var best = FindClosest<Targetable>(null,targetables,aim.normalized,transform.position);
          Select(targetables);
          Highlight(best);
          ScaleTime(Mathf.Lerp(1,.1f,aim.magnitude));
          LookAlong(aim.normalized);
          Move(Config.MoveSpeed * move,dt);
          if (action.PounceDown && best) {
            ClearSelected();
            ClearHighlighted();
            ScaleTime(1);
            Pounce(best);
          }
        } else {
          ClearSelected();
          ClearHighlighted();
          ScaleTime(1);
          LookAlong(move.magnitude > Config.AimThreshold ? move.normalized : transform.forward);
          Move(move.magnitude > Config.MoveThreshold ? Config.MoveSpeed * move : Vector3.zero,dt);
          if (action.PounceDown) {
            Jump(transform.forward);
          }
        }
      }
      break;

      case ApeState.Pouncing: {
        switch (ActionState) {
          case ActionState.Windup: {
            if (ActionFramesRemaining > 0) {
              ActionFramesRemaining--;
            } else {
              ActionFramesRemaining = Config.PounceConfig.ActiveFrames;
              ActionState = ActionState.Active;
            }
          };
          break;

          case ActionState.Active: {
            if (ActionFramesRemaining > 0) {
              Glide(ActionVelocity,dt);
              ActionFramesRemaining--;
            } else {
              if (Target && Grabbable(Target)) {
                Perch(PerchedOn);
                ActionFramesRemaining = Config.PounceConfig.RecoveryFrames;
                ActionState = ActionState.Recovery;
              } else if (CharacterController.isGrounded) {
                Target = null;
                PerchedOn = null;
                ActionFramesRemaining = Config.PounceConfig.RecoveryFrames;
                ActionState = ActionState.Recovery;
              } else {
                Target = null;
                PerchedOn = null;
                Fall();
              }
            }
          }
          break;

          // If we lose our perch at any time then detach from it
          case ActionState.Recovery: {
            if (ActionFramesRemaining > 0) {
              ActionFramesRemaining--;
            } else {
              PerchedOn = null;
              if (CharacterController.isGrounded) {
                State = ApeState.Moving;
              } else {
                Fall();
              }
            }
          }
          break;
        }
      }
      break;

      case ApeState.Perching: {
        if (aim.magnitude > 0) {
          var targetables = FindObjectsOfType<Targetable>(false);
          var current = PerchedOn.GetComponent<Targetable>();
          var best = FindClosest<Targetable>(current,targetables,aim.normalized,transform.position);
          ScaleTime(Mathf.Lerp(1,.1f,aim.magnitude));
          LookAlong(aim.normalized);
          Select(targetables);
          Highlight(best);
          if (action.PounceDown && best) {
            ClearSelected();
            ClearHighlighted();
            ScaleTime(1);
            Pounce(best);
          }
        } else {
          ClearSelected();
          ClearHighlighted();
          ScaleTime(1);
          LookAlong(move.magnitude > 0 ? move.normalized : transform.forward);
        }
      }
      break;
    }
  }
}