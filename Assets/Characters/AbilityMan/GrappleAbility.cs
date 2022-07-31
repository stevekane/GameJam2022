using System.Collections;
using UnityEngine;

enum GrappleState { Holding, Throwing, Pulling }

public class GrappleAbility : MonoBehaviour {
  public GameObject Owner;
  public GrapplingHook HookPrefab;
  public Timeval HookTravelDuration = Timeval.FromMillis(30, 30);
  public float Speed;
  public float ReleaseDistance = 1;

  GrapplingHook Hook;

  public void Activate() {
    Owner.GetComponent<Animator>().SetBool("Grappling", true);
    Owner.GetComponent<Animator>().SetInteger("GrappleState", (int)GrappleState.Holding);
    InputManager.Instance.L2.JustUp.Action += Throw;
    InputManager.Instance.R2.JustDown.Action += Stop;
  }

  public void Stop() {
    Owner.GetComponent<Animator>().SetBool("Grappling", false);
    Owner.GetComponent<Animator>().SetInteger("GrappleState", -1);
    InputManager.Instance.L2.JustUp.Action -= Throw;
    InputManager.Instance.R2.JustDown.Action -= Stop;
    if (Hook) {
      Hook.OnHit.Action -= Hit;
      Destroy(Hook.gameObject);
    }
    StopAllCoroutines();
  }

  void Throw() {
    InputManager.Instance.L2.JustUp.Action -= Throw;
    InputManager.Instance.R2.JustDown.Action -= Stop;
    Owner.GetComponent<Animator>().SetInteger("GrappleState", (int)GrappleState.Throwing);
    Hook = Instantiate(HookPrefab, transform.position, transform.rotation);
    Hook.Origin = transform;
    Hook.GetComponent<Rigidbody>().AddForce(Speed*transform.forward, ForceMode.Impulse);
    Hook.OnHit.Action += Hit;
    StartCoroutine(Wait(HookTravelDuration.Frames));
  }

  void Hit(Collision collision) {
    Hook.OnHit.Action -= Hit;
    Destroy(Hook.GetComponent<Collider>());
    Destroy(Hook.GetComponent<Rigidbody>());
    StopAllCoroutines();
    StartCoroutine(PullTowards(collision.GetContact(0).point));
  }

  IEnumerator Wait(int n) {
    for (var i = 0; i < n; i++) {
      yield return new WaitForFixedUpdate();
    }
    Stop();
  }

  IEnumerator PullTowards(Vector3 destination) {
    var frames = 0;
    Owner.GetComponent<Animator>().SetInteger("GrappleState", (int)GrappleState.Pulling);
    while (true) {
      var delta = destination-transform.position;
      var direction = delta.normalized;
      if (delta.magnitude < ReleaseDistance || frames > HookTravelDuration.Frames) {
        break;
      } else {
        frames++;
        Owner.GetComponent<CharacterController>().Move(Time.fixedDeltaTime*Speed*direction);
        yield return new WaitForFixedUpdate();
      }
    }
    Stop();
  }
}