using UnityEngine;
using UnityEngine.UI;

public class AimMeter : MonoBehaviour {
  public Slider FillRegion;

  public void SetFill(int maxValue, int value) {
    FillRegion.maxValue = maxValue;
    FillRegion.value = value;
  }
}