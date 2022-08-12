using System.Collections;
using UnityEngine;
using static Fiber;

public class GrappleAbility : Ability {
  enum GrappleState { Holding, Throwing, Pulling }
  enum ThrowResult { Hit, None }

  public Timeval MAX_CHARGE_DURATION = Timeval.FromMillis(3000);
  public Timeval MAX_THROW_DURATION = Timeval.FromMillis(1000);
  public Timeval MAX_PULL_DURATION = Timeval.FromMillis(1000);
  public float HOOK_SPEED = 150f;
  public float HOOK_RELEASE_DISTANCE = 1.5f;
  public GameObject Owner;
  public GrapplingHook HookPrefab;

  GrapplingHook Hook;

  public override void Stop() {
    if (Hook) {
      Destroy(Hook.gameObject);
    }
    Owner.GetComponent<Animator>().SetBool("Grappling", false);
    Owner.GetComponent<Animator>().SetInteger("GrappleState", -1);
    base.Stop();
  }

  public IEnumerator GrappleStart() {
    // Holding down the activation button
    Owner.GetComponent<Animator>().SetBool("Grappling", true);
    Owner.GetComponent<Animator>().SetInteger("GrappleState", (int)GrappleState.Holding);
    var chargeWait = Wait(MAX_CHARGE_DURATION.Frames);
    var release = ListenFor(AbilityManager.GetEvent(GrappleRelease));
    yield return Any(chargeWait, release);
    // Create and throw the hook
    Owner.GetComponent<Animator>().SetBool("Grappling", true);
    Owner.GetComponent<Animator>().SetInteger("GrappleState", (int)GrappleState.Throwing);
    Hook = Instantiate(HookPrefab, transform.position, transform.rotation);
    Hook.Owner = Owner;
    Hook.Origin = transform;
    Hook.OnHit.Action += OnHit;
    Hook.GetComponent<Rigidbody>().AddForce(HOOK_SPEED*transform.forward, ForceMode.Impulse);
    var hookHit = ListenFor(Hook.OnHit);
    var throwWait = Wait(MAX_THROW_DURATION.Frames);
    var throwOutcome = Select(hookHit, throwWait);
    yield return throwOutcome;
    Hook.OnHit.Action -= OnHit;
    // Hook hit something
    if (throwOutcome.Value == (int)ThrowResult.Hit) {
      Owner.GetComponent<Animator>().SetBool("Grappling", true);
      Owner.GetComponent<Animator>().SetInteger("GrappleState", (int)GrappleState.Pulling);
      var contactPoint = hookHit.Value.GetContact(0).point;
      var pullWait = Wait(MAX_PULL_DURATION.Frames);
      var pullComplete = PullTowards(Owner, contactPoint, HOOK_SPEED, HOOK_RELEASE_DISTANCE);
      yield return Any(pullWait, pullComplete);
    }
    Stop();
  }

  public IEnumerator GrappleRelease() => null;

  void OnHit(Collision c) {
    Destroy(Hook.GetComponent<Rigidbody>());
    Destroy(Hook.GetComponent<Collider>());
  }

  IEnumerator PullTowards(GameObject subject, Vector3 destination, float speed, float releaseDistance) {
    while (subject) {
      var delta = destination-subject.transform.position;
      if (delta.magnitude > releaseDistance) {
        var direction = delta.normalized;
        var displacement = Time.fixedDeltaTime*speed*direction;
        Debug.DrawRay(subject.transform.position, displacement);
        subject.GetComponent<CharacterController>().Move(displacement);
        yield return null;
      } else {
        yield break;
      }
    }
  }
}