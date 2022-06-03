using UnityEngine;

public class Ape : MonoBehaviour {
  public enum ApeState { Dead, Falling, Jumping, Moving, Perching, Pouncing, Stomping }  

  [Header("Configuration")]
  [SerializeField] PlayerConfig Config;

  [Header("Components")]
  [SerializeField] CharacterController CharacterController;
  
  [Header("User Interface")]
  [SerializeField] Selector Selector;
  [SerializeField] Selector[] Selectors;

  [Header("State")]
  public ApeState State = ApeState.Moving;
  public Vector3 JumpDirection;
  public float FallTimeRemaining;
  public float JumpTimeRemaining;
  public float AimingTimeRemaining;
  public float PounceTimeRemaining;
  public float StompTimeRemaining;
  public Holdable Held;
  public Targetable Target;
  public Perchable PerchedOn;

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
        Selectors[i].Target = components[i].transform;
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
      Selector.Target = component.transform;
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

  void Jump(Vector3 direction, float duration) {
    JumpTimeRemaining = duration;
    JumpDirection = transform.forward;
    State = ApeState.Jumping;
  }

  void Fall(float duration) {
    FallTimeRemaining = duration;
    State = ApeState.Falling;
  }

  void Pounce(Targetable target, float duration) {
    transform.position = target.transform.position;
    Target = target;
    PounceTimeRemaining = Config.PounceDuration;
    State = ApeState.Pouncing;
  }

  void Die() {
    JumpDirection = Vector3.zero;
    FallTimeRemaining = 0;
    JumpTimeRemaining = 0;
    AimingTimeRemaining = 0;
    Held = null;
    Target = null;
    PerchedOn = null;
    State = ApeState.Dead;
  }

  void Start() {
    State = ApeState.Moving;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var move = new Vector3(action.Move.x,0,action.Move.y);
    var aim = new Vector3(action.Aim.x,0,action.Aim.y);

    switch (State) {
      case ApeState.Falling: {
        if (CharacterController.isGrounded) {
          State = ApeState.Moving;
        } else if (FallTimeRemaining > 0) {
          FallTimeRemaining -= dt; 
          Drop(dt);
        } else {
          Die();
        }
      }      
      break;

      case ApeState.Jumping: {
        var jumpSpeed = Config.JumpDistance / Config.JumpDuration;
        if (JumpTimeRemaining > dt) {
          JumpTimeRemaining -= dt;
          Glide(jumpSpeed * JumpDirection,dt);
        } else {
          JumpTimeRemaining = 0;
          Glide(jumpSpeed * JumpDirection,JumpTimeRemaining);
          Drop(dt - JumpTimeRemaining);
          if (CharacterController.isGrounded) {
            State = ApeState.Moving;
          } else {
            Fall(Config.FallDuration);
          }
        }
      }
      break;

      case ApeState.Moving: {
        if (!CharacterController.isGrounded) {
          Time.timeScale = 1;
          Drop(dt);
          ClearSelected();
          ClearHighlighted();
          Fall(Config.FallDuration);
        } else if (aim.magnitude > 0) {
          var targetables = FindObjectsOfType<Targetable>(false);
          var best = FindClosest<Targetable>(null,targetables,aim.normalized,transform.position);
          Time.timeScale = Inputs.InPlayBack ? 1 : Mathf.Lerp(1,.1f,aim.magnitude);
          Move(Config.MoveSpeed * move,dt);
          LookAlong(aim.normalized);
          Select(targetables);
          Highlight(best);
          if (action.Pounce && best) {
            ClearSelected();
            ClearHighlighted();
            Pounce(best,Config.PounceDuration);
          }
        } else {
          Time.timeScale = 1;
          Move(Config.MoveSpeed * move,dt);
          LookAlong(move.magnitude > 0 ? move.normalized : transform.forward);
          ClearSelected();
          ClearHighlighted();
          if (action.Pounce) {
            Jump(transform.forward,Config.JumpDuration);
          }
        }
      }
      break;

      case ApeState.Pouncing: {
        if (Target.TryGetComponent(out Perchable perchable)) {
          // TODO: perchable.PounceOn(this);
          PerchedOn = perchable;
          State = ApeState.Perching;
        } else {
          Fall(Config.FallDuration);
        }
      }
      break;

      case ApeState.Stomping: {
        if (Target.TryGetComponent(out Perchable perchable)) {
          // TODO: perchable.StompOn(this);
          PerchedOn = perchable;
          State = ApeState.Perching;
        } else {
          Fall(Config.FallDuration);
        }
      }
      break;

      case ApeState.Perching: {
        if (aim.magnitude > 0) {
          var targetables = FindObjectsOfType<Targetable>(false);
          var current = PerchedOn.GetComponent<Targetable>();
          var best = FindClosest<Targetable>(current,targetables,aim.normalized,transform.position);
          Time.timeScale = Inputs.InPlayBack ? 1 : Mathf.Lerp(1,.1f,aim.magnitude);
          LookAlong(aim.normalized);
          Select(targetables);
          Highlight(best);
          if (action.Pounce && best) {
            ClearSelected();
            ClearHighlighted();
            Pounce(best,Config.PounceDuration);
          }
        } else {
          Time.timeScale = 1;
          LookAlong(move.magnitude > 0 ? move.normalized : transform.forward);
          ClearSelected();
          ClearHighlighted();
        }
      }
      break;
    }
  }
}