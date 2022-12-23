using UnityEngine;

[RequireComponent(typeof(Status))]
public class Defender : MonoBehaviour {
  Status Status;
  bool PlayingFallSound;
  bool Died = false;
  public Vector3? LastGroundedPosition { get; private set; }
  public Hurtbox[] Hurtboxes;

  void Awake() {
    Status = GetComponent<Status>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    Hurtboxes.ForEach(hb => hb.gameObject.SetActive(Status.IsHittable));
    if (transform.position.y < -1f && !PlayingFallSound) {
      LastGroundedPosition = transform.position;
      PlayingFallSound = true;
      SFXManager.Instance.TryPlayOneShot(SFXManager.Instance.FallSFX);
    }
    if (transform.position.y < -30f) {
      Die();
    }
  }

  public void Die() {
    if (Died)
      return;
    Died = true;
    // TODO: keep track of last attacker
    LastGroundedPosition = LastGroundedPosition ?? transform.position;
    SendMessage("OnDeath", SendMessageOptions.RequireReceiver);
  }
}