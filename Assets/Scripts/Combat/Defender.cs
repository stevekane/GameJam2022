using UnityEngine;

[RequireComponent(typeof(Status))]
public class Defender : MonoBehaviour {
  Status Status;
  bool PlayedFallSound = false;
  bool Died = false;
  public Vector3? LastGroundedPosition { get; private set; }
  public Hurtbox[] Hurtboxes;
  LevelBounds LevelBounds;

  void Awake() {
    this.InitComponent(out Status);
    LevelBounds = FindObjectOfType<LevelBounds>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    Hurtboxes.ForEach(hb => hb.gameObject.SetActive(Status.IsHittable));
    if (transform.position.y < LevelBounds.Bottom+10f && !PlayedFallSound) {
      LastGroundedPosition = transform.position;
      PlayedFallSound = true;
      SFXManager.Instance.TryPlayOneShot(SFXManager.Instance.FallSFX);
    }
    if (Status.IsGrounded)
      PlayedFallSound = false;
    if (LevelBounds && !LevelBounds.IsInBounds(transform.position)) {
      Die();
    }
  }

  public void Die() {
    if (Died)
      return;
    Died = true;
    // TODO: keep track of last attacker
    LastGroundedPosition = LastGroundedPosition ?? transform.position;
    SendMessage("OnDeath", LevelBounds.GetIntersectionNormal(transform.position), SendMessageOptions.RequireReceiver);
  }
}