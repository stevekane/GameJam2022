using System;
using UnityEngine;

public class DieOnHit : MonoBehaviour {
  public float Health = 10f;

  void OnHurt(HitParams hitParams) {
    Health -= hitParams.Damage;
    if (Health <= 0f) {
      Die();
    }
  }

  public void Die() {
    SendMessage("OnDeath", Vector3.up, SendMessageOptions.RequireReceiver);
    Destroy(gameObject);
  }
}