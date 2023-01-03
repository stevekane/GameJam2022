using UnityEngine;

namespace Traditional {
  public class FootSteps : MonoBehaviour {
    [SerializeField] AudioClip SFX;
    [SerializeField] GameObject VFX;
    [SerializeField] AudioSource Source;

    void OnFootStep(FootHit hit) {
      Source.PlayOptionalOneShot(SFX);
      const float LIFETIME = 2;
      var position = hit.Foot.transform.position;
      var orientation = Quaternion.LookRotation(hit.Foot.transform.forward.XZ(), Vector3.up);
      Destroy(Instantiate(VFX, position, orientation), LIFETIME);
    }
  }
}