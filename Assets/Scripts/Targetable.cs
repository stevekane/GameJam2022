using UnityEngine.Events;
using UnityEngine;

public class Targetable : MonoBehaviour {
  public float Height;

  [SerializeField] UnityEvent<Hero> OnPounceTo;
  [SerializeField] UnityEvent<Hero> OnPounceFrom;

  public void PounceTo(Hero hero) {
    Debug.Log("Pounce To");
    OnPounceTo.Invoke(hero);
  }
  public void PounceFrom(Hero hero) {
    Debug.Log("Pounce From");
    OnPounceFrom.Invoke(hero);
  }
}