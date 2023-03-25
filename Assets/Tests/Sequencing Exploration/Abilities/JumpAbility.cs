using System.Threading.Tasks;
using UnityEngine;

public class JumpAbility : MonoBehaviour {
  [SerializeField] AudioClip SFX;
  [SerializeField] int StartupTicks;
  [SerializeField] float JumpStrength;
  [SerializeField] Velocity Velocity;
  [SerializeField] Animator Animator;
  [SerializeField] AudioSource AudioSource;

  public async Task Jump(TaskScope scope) {
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    for (var i = 0; i < StartupTicks; i++) {
      await scope.ListenFor(FixedFrame.Instance.TickEvent);
    }
    Velocity.Value.y = JumpStrength;
  }
}