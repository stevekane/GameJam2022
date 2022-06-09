using UnityEngine;

public class Targetable : MonoBehaviour {
  public float Height = 1;
  public float Radius = 1;

  public void PounceTo(Hero hero) {
    GetComponent<Mob>()?.OnPounceTo(hero);
  }
  public void PounceFrom(Hero hero) {
    GetComponent<Mob>()?.OnPounceFrom(hero);
  }
}