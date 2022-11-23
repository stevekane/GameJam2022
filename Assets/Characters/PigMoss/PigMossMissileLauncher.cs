using UnityEngine;

public class PigMossMissileLauncher : MonoBehaviour {
  [SerializeField] AudioClip WindupClip;
  [SerializeField] AudioClip ShotClip;
  [SerializeField] AudioClip RecoveryClip;
  [SerializeField] GameObject ShotEffect;
  [SerializeField] Animator Animator;

  public void OnBombardWindup() {
    Debug.Log("Windup");
    SFXManager.Instance.TryPlayOneShot(WindupClip);
    Animator.SetBool("Extended", true);
  }
  public void OnBombardShot(Transform t) {
    SFXManager.Instance.TryPlayOneShot(ShotClip);
    VFXManager.Instance.TrySpawnEffect(ShotEffect, t.position);
    Debug.Log($"Shot from {t.name}");
  }
  public void OnBombardRecovery() {
    Debug.Log("Recovery");
    SFXManager.Instance.TryPlayOneShot(RecoveryClip);
    Animator.SetBool("Extended", false);
  }
  public void OnBombardStop() {
    Debug.Log("Stop");
    Animator.SetBool("Extended", false);
  }
}