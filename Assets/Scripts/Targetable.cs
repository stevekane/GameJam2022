using UnityEngine.Events;
using UnityEngine;

public class Targetable : MonoBehaviour {
  public float Height;

  public void PounceTo(Hero hero) {
    Debug.Log("Pounce To");
    GetComponent<Mob>()?.OnPounceTo(hero);
  }
  public void PounceFrom(Hero hero) {
    Debug.Log("Pounce From");
    GetComponent<Mob>()?.OnPounceFrom(hero);
  }
}