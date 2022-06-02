using System.Collections.Generic;
using UnityEngine;

public class Ape : MonoBehaviour {
  public enum ApeState { Moving, Action, Pouncing, Perched, PerchedAction }  

  [SerializeField] CharacterController CharacterController;
  [SerializeField] PlayerConfig Config;
  [SerializeField] Selector Selector;
  [SerializeField] LayerMask TargetLayerMask;

  public ApeState State;
  float BulletTimeMax = 6;
  float BulletTimeRemaining = 6;
  Transform Origin = null;
  Transform Target = null;
  Transform PerchedOn = null;
  Vector3 PounceDestination;
  List<GameObject> Targets = new List<GameObject>(3);

  float Score(Vector3 forward, Vector3 origin, Vector3 target) {
    var delta = target - origin;
    var distance = delta.magnitude;
    var dot = distance > 0 ? Vector3.Dot(delta.normalized,forward) : 1;
    var a = Config.DistanceScore.Evaluate(1 - distance / Config.SearchRadius);
    var b = Config.AngleScore.Evaluate(Mathf.Lerp(0,1,Mathf.InverseLerp(-1,1,dot)));
    return a + b;
  }

  Transform FindClosestTarget(Transform ignore,Collider[] colliders, Vector3 forward, Vector3 origin) {
    Transform bestTransform = null;
    float bestScore = 0;
    for (int i = 0; i < colliders.Length; i++) {
      var score = Score(forward,origin,colliders[i].transform.position);
      if (score > bestScore && colliders[i].transform != ignore) {
        bestScore = score;
        bestTransform = colliders[i].transform;
      }
    }
    return bestTransform;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var move = new Vector3(action.Move.x,0,action.Move.y);
    var aim = new Vector3(action.Aim.x,0,action.Aim.y);
    var tryMove = move.magnitude > 0;
    var tryAim = aim.magnitude > 0;
    var tryDilate = tryAim && BulletTimeRemaining > 0 && !Inputs.InPlayBack;
    var colliders = Physics.OverlapSphere(transform.position,Config.SearchRadius,TargetLayerMask);
    var best = FindClosestTarget(PerchedOn,colliders,aim.normalized,transform.position);

    switch (State) {
      case ApeState.Moving: {
        CharacterController.Move(dt * Config.MoveSpeed * move);
        if (tryAim) {
          State = ApeState.Action;
        }
      }
      break;

      case ApeState.Action: {
        CharacterController.Move(dt * Config.MoveSpeed * move);
        Time.timeScale = tryDilate ? Mathf.Lerp(1,.1f,aim.magnitude) : 1;
        if (action.Pounce && Target) {
          CharacterController.Move(Target.position - transform.position);
          Selector.gameObject.SetActive(false);
          Selector.Target = null;
          State = ApeState.Pouncing;
        } else if (!tryAim) {
          Selector.gameObject.SetActive(false);
          Selector.Target = null;
          State = ApeState.Moving;
        } else {
          Selector.gameObject.SetActive(best);
          Selector.Target = best;
          Target = best;
          var rotation = transform.rotation;
          rotation.SetLookRotation(aim);
          transform.rotation = rotation;
        }
      }
      break;

      case ApeState.Pouncing: {
        PerchedOn = Target;
        State = ApeState.Perched;
      }
      break;

      case ApeState.Perched: {
        if (tryAim) {
          State = ApeState.PerchedAction;
        }
      }
      break;

      case ApeState.PerchedAction: {
        Time.timeScale = tryDilate ? Mathf.Lerp(1,.1f,aim.magnitude) : 1;
        if (action.Pounce && Target) {
          CharacterController.Move(Target.position - transform.position);
          Selector.gameObject.SetActive(false);
          Selector.Target = null;
          State = ApeState.Pouncing;
        } else if (action.Hit && Target) {
          Debug.Log("Wack Wack Wack Wack!!!!!!!!!!");
        } else if (!tryAim) {
          Selector.gameObject.SetActive(false);
          Selector.Target = null;
          State = ApeState.Perched;
        } else {
          Selector.gameObject.SetActive(best);
          Selector.Target = best;
          Target = best;
          var rotation = transform.rotation;
          rotation.SetLookRotation(aim);
          transform.rotation = rotation;
        }
      }
      break;
    }
  }
}