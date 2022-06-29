using UnityEngine;
using UnityEngine.Events;

public class Hurtbox : MonoBehaviour {
  [SerializeField]
  UnityEvent<Hitbox> OnHurt;
}