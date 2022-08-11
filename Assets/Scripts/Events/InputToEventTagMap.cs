using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ButtonEventTagMap {
  public ButtonCode ButtonCode;
  public ButtonPressType ButtonPressType;
  public EventTag EventTag;
}

[Serializable]
public class AxisEventTagMap {
  public AxisCode AxisCode;
  public EventTag EventTag;
}

[RequireComponent(typeof(AbilityManager))]
public class InputToEventTagMap : MonoBehaviour {
  [SerializeField] List<ButtonEventTagMap> ButtonEventTagMaps;
  [SerializeField] List<AxisEventTagMap> AxisEventTagMaps;

  // TODO: This is where we should grab actual eventsources or axes by reference from InputManager.Instance
  // The AbilityManager should have no idea where these things are coming from
  void Start() {
    var AbilityManager = GetComponent<AbilityManager>();
    ButtonEventTagMaps.ForEach(btm => AbilityManager.RegisterTag(btm.EventTag, btm.ButtonCode, btm.ButtonPressType));
    AxisEventTagMaps.ForEach(atm => AbilityManager.RegisterTag(atm.EventTag, atm.AxisCode));
  }
}