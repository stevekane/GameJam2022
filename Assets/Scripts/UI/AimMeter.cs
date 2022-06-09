using UnityEngine;
using UnityEngine.UI;

public class AimMeter : MonoBehaviour {
  public Transform Target;
  public Slider FillRegion;
  public float Height = 1;

  public void SetFill(int maxValue, int value) {
    FillRegion.maxValue = maxValue;
    FillRegion.value = value;
  }
}