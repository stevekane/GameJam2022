using System;
using UnityEngine;

public class SaveObject : MonoBehaviour {
  public void RegisterSaveable(Component component) {
    GetComponent<ES3AutoSave>().componentsToSave.Add(component);
  }

  public void DispatchOnBeforeSave() {
    SendMessage("OnBeforeSave", SendMessageOptions.DontRequireReceiver);
  }

  void Awake() {
    GetComponent<ES3AutoSave>().componentsToSave.Add(this);
    GetComponent<ES3AutoSave>().componentsToSave.Add(transform);
  }
}