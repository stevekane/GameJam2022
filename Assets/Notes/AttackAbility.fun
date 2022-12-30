Translate AttackAbility to experimental VM syntax.

The goal here is to identify how you might build a VM for a language
that interops with Unity GameObjects while offering a process-oriented
design, private mutable state, and GameObject/Scene-aware lifecycle
guarantees. In particular, you want processes to always die when
the object that owns them dies. Additionally, you want all processes
to form a tree such that if the root process is killed, every child
process is killed. Finally, you want easy interop with C#/Unity MonoBehaviour
such that configuration or data may be easily specified in C# and used
naturally inside the running VM.

OnHit :: hitParams: HitParams -> hitbox: TriggerEvent -> hits:Set HurtBox ->
  collider <- hitbox.OnTriggerStay
  UNLESS collider.hurtbox ∈ hits
    collider.hurtbox.TryAttack[hitParams]

Attack : hitConfig: HitConfig -> hitbox: TriggerEvent -> AnimationConfig animationConfig
  play(animationConfig, windup)
  RACE
    + play(animationConfig, active)
    + hitbox::on(hit)
  add Cnacellable to Tags
  animation.Done