using UnityEngine.Events;
using UnityEngine;

public class Targetable : MonoBehaviour {
  public float Height;

  public void PounceTo(Hero hero) {
    GetComponent<Mob>()?.OnPounceTo(hero);
  }
  public void PounceFrom(Hero hero) {
    GetComponent<Mob>()?.OnPounceFrom(hero);
  }
}