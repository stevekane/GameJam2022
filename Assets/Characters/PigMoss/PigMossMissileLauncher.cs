using UnityEngine;

public class PigMossMissileLauncher : MonoBehaviour {
  [SerializeField] AudioClip WindupClip;
  [SerializeField] AudioClip ShotClip;
  [SerializeField] AudioClip RecoveryClip;
  [SerializeField] GameObject ShotEffect;
  [SerializeField] Animator Animator;

  public void OnBombardWindup() {
    SFXManager.Instance.TryPlayOneShot(WindupClip);
    Animator.SetBool("Extended", true);
  }
  public void OnBombardShot(Transform t) {
    SFXManager.Instance.TryPlayOneShot(ShotClip);
    VFXManager.Instance.TrySpawnEffect(ShotEffect, t.position);
  }
  public void OnBombardRecovery() {
    SFXManager.Instance.TryPlayOneShot(RecoveryClip);
    Animator.SetBool("Extended", false);
  }
  public void OnBombardStop() {
    Animator.SetBool("Extended", false);
  }
}