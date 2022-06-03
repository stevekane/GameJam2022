using UnityEngine;

public class Ape : MonoBehaviour {
  public enum ApeState { Dead, Falling, Jumping, Moving, Perching, Pouncing, Stomping }  

  [SerializeField] CharacterController CharacterController;
  [SerializeField] PlayerConfig Config;
  [SerializeField] Selector Selector;

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

  T FindClosest<T>(T ignore, Collider[] colliders, Vector3 forward, Vector3 origin) where T : MonoBehaviour {
    T best = null;
    var bestScore = 0f;
    for (int i = 0; i < colliders.Length; i++) {
      var targetable = colliders[i].GetComponent<T>();
      var score = Score(forward,origin,colliders[i].transform.position);
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

  void Start() {
    State = ApeState.Moving;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var move = new Vector3(action.Move.x,0,action.Move.y);
    var aim = new Vector3(action.Aim.x,0,action.Aim.y);
    var tryMove = move.magnitude > 0;
    var tryAim = aim.magnitude > 0;
    var tryDilate = tryAim && !Inputs.InPlayBack;
    // var colliders = Physics.OverlapSphere(transform.position,Config.SearchRadius);
    // var best = FindClosest<Targetable>(PerchedOn.GetComponent<Targetable>(),colliders,aim.normalized,transform.position);

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
        Move(Config.MoveSpeed * move,dt);
        LookAlong(move.magnitude > 0 ? move.normalized : transform.forward);
        if (aim.magnitude > 0) {
          var colliders = Physics.OverlapSphere(transform.position,Config.SearchRadius,LayerMask.GetMask("Targets"));
          // var best = FindClosest<Targetable>(PerchedOn.GetComponent<Targetable>(),colliders,aim.normalized,transform.position);
          foreach (var c in colliders) {
            Debug.DrawLine(transform.position + Vector3.up,c.transform.position);
          }
          CharacterController.Move(dt * Config.MoveSpeed * move);
          Time.timeScale = tryDilate ? Mathf.Lerp(1,.1f,aim.magnitude) : 1;
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
          Time.timeScale = tryDilate ? Mathf.Lerp(1,.1f,aim.magnitude) : 1;
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