using System.Threading.Tasks;
using UnityEngine;

public class DoubleJumpAbility : MonoBehaviour {
  [SerializeField] AudioClip SFX;
  [SerializeField] float JumpStrength;
  [SerializeField] Animator Animator;
  [SerializeField] Velocity Velocity;
  [SerializeField] AudioSource AudioSource;

  public async Task Jump(TaskScope scope) {
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    Velocity.Value.y = JumpStrength;
  }
}