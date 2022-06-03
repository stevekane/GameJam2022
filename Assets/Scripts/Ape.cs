using UnityEngine;

public class Ape : MonoBehaviour {
  public enum ApeState { Dead, Falling, Jumping, Moving, Perching, Pouncing, Stomping }  

  [SerializeField] CharacterController CharacterController;
  [SerializeField] PlayerConfig Config;
  
  [Header("User Interface")]
  [SerializeField] Selector Selector;
  [SerializeField] Selector[] Selectors;

  [Header("Root States")]
  public ApeState State = ApeState.Moving;

  [Header("Jumping")]
  public Vector3 JumpDirection;
  public float JumpTimeRemaining;

  [Header("Holding")]
  public Holdable Held;

  [Header("Aiming")]
  float AimingTimeRemaining;

  [Header("Targeting")]
  public Targetable Target;

  [Header("Perching")]
  public Perchable PerchedOn;

  [Header("Pouncing")]
  public Targetable PounceTarget;

  [Header("Stomping")]
  public Targetable StompTarget;

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

  void Glide(Vector3 magnitude, float dt) {
    CharacterController.Move(magnitude * dt);
  }

  void Move(Vector3 magnitude, float dt) {
    CharacterController.Move((magnitude + Physics.gravity) * dt);
  }

  void Fall(float dt) {
    CharacterController.Move(Physics.gravity * dt);
  }

  void LookAlong(Vector3 forward) {
    transform.rotation = Quaternion.LookRotation(forward,Vector3.up);
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

  void Start() {
    State = ApeState.Moving;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var move = new Vector3(action.Move.x,0,action.Move.y);
    var aim = new Vector3(action.Aim.x,0,action.Aim.y);

    // Global rule across states
    if (transform.position.y <= -10) {
      // TODO: if (Held) Held.Drop()
      Held = null;
      Target = null;
      PerchedOn = null;
      PounceTarget = null;
      StompTarget = null;
      State = ApeState.Dead;
    }

    /*
    TODO: Global rule for dying holdables
    if (Held.Dead)  {
      Held = null;
    }
    */

    switch (State) {
      case ApeState.Falling: {
        Fall(dt);
        if (CharacterController.isGrounded) {
          State = ApeState.Moving;
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
          Fall(dt - JumpTimeRemaining);
          if (CharacterController.isGrounded) {
            State = ApeState.Moving;
          } else {
            State = ApeState.Falling;
          }
        }
      }
      break;

      case ApeState.Moving: {
        if (!CharacterController.isGrounded) {
          Time.timeScale = 1;
          Target = null;
          Fall(dt);
          ClearSelected();
          ClearHighlighted();
          State = ApeState.Falling;
        } else if (aim.magnitude > 0) {
          var targetables = FindObjectsOfType<Targetable>(false);
          var best = FindClosest<Targetable>(null,targetables,aim.normalized,transform.position);
          Time.timeScale = Inputs.InPlayBack ? 1 : Mathf.Lerp(1,.1f,aim.magnitude);
          Move(Config.MoveSpeed * move,dt);
          LookAlong(aim.normalized);
          Select(targetables);
          Highlight(best);
          // if (action.Pounce && Target) {
          //   CharacterController.Move(Target.transform.position - transform.position);
          //   Selector.gameObject.SetActive(false);
          //   Selector.Target = null;
          //   State = ApeState.Pouncing;
          // } else if (!tryAim) {
          //   Selector.gameObject.SetActive(false);
          //   Selector.Target = null;
          //   State = ApeState.Moving;
          // } else {
          //   Selector.gameObject.SetActive(best);
          //   Selector.Target = best.transform;
          //   Target = best;
          //   var rotation = transform.rotation;
          //   rotation.SetLookRotation(aim);
          //   transform.rotation = rotation;
          // }
        } else {
          Time.timeScale = 1;
          Target = null;
          Move(Config.MoveSpeed * move,dt);
          LookAlong(move.magnitude > 0 ? move.normalized : transform.forward);
          ClearSelected();
          ClearHighlighted();
          if (action.Pounce) {
            JumpTimeRemaining = Config.JumpDuration;
            JumpDirection = transform.forward;
            State = ApeState.Jumping;
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
          Debug.Log("Fell no perch available!");
          State = ApeState.Falling;
        }
      }
      break;

      case ApeState.Stomping: {
        if (Target.TryGetComponent(out Perchable perchable)) {
          // TODO: perchable.StompOn(this);
          PerchedOn = perchable;
          State = ApeState.Perching;
        } else {
          Debug.Log("Fell no perch available!");
          State = ApeState.Falling;
        }
      }
      break;

      case ApeState.Perching: {
        if (aim.magnitude > 0) {
          Time.timeScale = Inputs.InPlayBack ? 1 : Mathf.Lerp(1,.1f,aim.magnitude);
          // if (action.Pounce && Target) {
          //   CharacterController.Move(Target.transform.position - transform.position);
          //   Selector.gameObject.SetActive(false);
          //   Selector.Target = null;
          //   State = ApeState.Pouncing;
          // } else if (action.Hit && Target) {
          //   Debug.Log("Wack Wack Wack Wack!!!!!!!!!!");
          // } else if (!tryAim) {
          //   Selector.gameObject.SetActive(false);
          //   Selector.Target = null;
          //   State = ApeState.Perching;
          // } else {
          //   Selector.gameObject.SetActive(best);
          //   Selector.Target = best.transform;
          //   Target = best;
          //   var rotation = transform.rotation;
          //   rotation.SetLookRotation(aim);
          //   transform.rotation = rotation;
          // }
        }
      }
      break;
    }
  }
}