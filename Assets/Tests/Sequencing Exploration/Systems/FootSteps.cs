using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FootSteps : MonoBehaviour {
  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] AvatarAttacher AvatarAttacher;
  [SerializeField] GameObject VFX;
  [SerializeField] AudioClip SFX;
  [SerializeField] DecalProjector Decal;

  Transform LeftFoot;
  Transform RightFoot;

  void Awake() {
    LeftFoot = AvatarAttacher.GetBoneTransform(AvatarBone.LeftFoot);
    RightFoot = AvatarAttacher.GetBoneTransform(AvatarBone.RightFoot);
  }

  void OnFootStep(string footName) {
    var targetFoot = footName == "Left" ? LeftFoot : RightFoot;
    if (MovementSpeed.Value > 10) {
      Destroy(Instantiate(VFX, targetFoot.position, targetFoot.rotation).gameObject, 2);
      Destroy(Instantiate(Decal, targetFoot.position, Quaternion.LookRotation(Vector3.down)).gameObject, 2);
    }
    AudioSource.PlayClipAtPoint(SFX, targetFoot.position);
  }
}