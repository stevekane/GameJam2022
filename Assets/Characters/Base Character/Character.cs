using UnityEngine;

[RequireComponent(typeof(AbilityManager))]
[RequireComponent(typeof(AvatarAttacher))]
[RequireComponent(typeof(AnimationDriver))]
[RequireComponent(typeof(Attributes))]
[RequireComponent(typeof(Mover))]
[RequireComponent(typeof(Status))]
[RequireComponent(typeof(Damage))]
[RequireComponent(typeof(Defender))]
public class Character : MonoBehaviour {
  [HideInInspector] public AbilityManager AbilityManager;
  [HideInInspector] public AvatarAttacher AvatarAttacher;
  [HideInInspector] public AnimationDriver AnimationDriver;
  [HideInInspector] public Attributes Attributes;
  [HideInInspector] public Mover Mover;
  [HideInInspector] public Status Status;
  [HideInInspector] public Damage Damage;
  [HideInInspector] public Defender Defender;

  void Awake() {
    this.InitComponent(out AbilityManager);
    this.InitComponent(out AvatarAttacher);
    this.InitComponent(out AnimationDriver);
    this.InitComponent(out Attributes);
    this.InitComponent(out Mover);
    this.InitComponent(out Status);
    this.InitComponent(out Damage);
    this.InitComponent(out Defender);
  }
}