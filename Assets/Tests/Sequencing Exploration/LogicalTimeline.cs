using UnityEngine;
using UnityEngine.Playables;

public class LogicalTimeline : MonoBehaviour {
  public static int FixedFrame;
  public static EventSource FixedTick = new();

  [Header("Input")]
  [SerializeField] InputManager InputManager;
  [SerializeField] float MovementSpeed = 10;

  [Header("Visual Effects")]
  [SerializeField] GameObject OnHitVFX;

  [Header("Components")]
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] MeleeAttackAbility ThreeHitComboAbility;
  [SerializeField] MeleeAttackTargeting MeleeAttackTargeting;

  TaskScope Scope;

  public PlayableGraph Graph;
  public float LocalTimeScale = 1;
  public int HitStopFramesRemaining;

  void Start() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    Scope = new TaskScope();
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(StartAttack);
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(StartAttack);
    Scope.Dispose();
    Graph.Destroy();
  }

  /*
  A few things here I'd like to test.

    Multi-target hitting
    Target selection
    Target stickiness
    Multi-target with interuption
    Stick steering with threshold requirement to select next candidate target
  */

  void FixedUpdate() {
    var dt = LocalTimeScale * Time.fixedDeltaTime;
    var movementInput = InputManager.Axis(AxisCode.AxisLeft);
    var screenDirection = movementInput.XY;
    var movementMagnitude = screenDirection.magnitude;
    var camera = Camera.main; // TODO: slow way to access camera
    var worldSpaceDirection = camera.transform.TransformDirection(screenDirection);
    worldSpaceDirection.y = 0;
    worldSpaceDirection = worldSpaceDirection.normalized;
    // TODO: Need some other way of detecting if the character is controllable or not
    // if (Phase == AttackPhase.None) {
    //   if (movementMagnitude > 0) {
    //     transform.rotation = Quaternion.LookRotation(worldSpaceDirection);
    //   }
    //   Controller.Move(dt * movementMagnitude * MovementSpeed * worldSpaceDirection);
    // }

    LocalTimeScale = HitStopFramesRemaining > 0 ? 0 : 1;
    HitStopFramesRemaining = Mathf.Max(0, HitStopFramesRemaining-1);
    Graph.Evaluate(dt);
    FixedFrame++;
    FixedTick.Fire();
  }

  void OnHit(MeleeContact contact) {
    var damageString = contact.Hitbox.HitboxParams.Damage.ToString();
    WorldSpaceMessageManager.Instance.SpawnMessage(damageString, contact.Hurtbox.transform.position + 2 * Vector3.up);
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks;
  }

  void OnBlocked(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2;
  }

  void OnParried(MeleeContact contact) {
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHurt(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2;
    Scope.Dispose();
    Scope = new();
    Debug.Log("OnParried");
    Animator.SetTrigger("Parried");
  }

  void StartAttack() {
    Scope.Dispose();
    Scope = new();
    InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
    Scope.Start(ThreeHitComboAbility.Attack);
  }
}