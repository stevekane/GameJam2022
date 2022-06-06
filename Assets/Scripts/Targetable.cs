using UnityEngine.Events;
using UnityEngine;

public class Targetable : MonoBehaviour {
  public float Height;

  [SerializeField] UnityEvent<Ape> OnPounceTo;
  [SerializeField] UnityEvent<Ape> OnPounceFrom;

  public void PounceTo(Ape ape) {
    OnPounceTo.Invoke(ape);
  }
  public void PounceFrom(Ape ape) {
    OnPounceFrom.Invoke(ape);
  }
}