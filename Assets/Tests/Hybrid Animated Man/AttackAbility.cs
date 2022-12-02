using System.Collections;
using UnityEngine;

public class AttackAbility : Ability {
  [SerializeField] AnimationDriver AnimationDriver;
  [SerializeField] PlayableAnimation AttackAnimation;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;

  public IEnumerator Attack() {
    SFXManager.Instance.TryPlayOneShot(AttackSFX);
    VFXManager.Instance.TrySpawn2DEffect(AttackVFX, transform.position+Vector3.up, transform.rotation);
    yield return AnimationDriver.Play(AttackAnimation);
  }

  public override void OnStop() {
  }
}