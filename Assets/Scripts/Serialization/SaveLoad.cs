using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Represents all the save data we read/write to the save file.
public class SaveData {
  public List<UpgradeData> Upgrades;
  public int Gold;

  static string Key(int slot) => $"game{slot}";

  // TODO: use async IO
  public static void SaveToFile(int slot) {
    try {
      Save3(Key(slot));
      Debug.Log($"Saved");
    } catch (Exception e) {
      Debug.Log($"Save failed");
      Debug.LogException(e);
    }
  }
  public static void LoadFromFile(int slot) {
    try {
      ResetLiveData();
      Load3(Key(slot));
    } catch (Exception e) {
      Debug.Log($"Load failed");
      Debug.LogException(e);
    }
  }

  static void Save3(string key) {
    SaveData data = new();
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Save(data);
    var objs = UnityEngine.Object.FindObjectsOfType<SaveObject>().Select(b => b.gameObject).ToList();
    var settings = new ES3Settings { memberReferenceMode = ES3.ReferenceMode.ByRef };
    ES3.Save(key, data, settings);
    ES3.Save(key + "objects", objs, settings);
  }
  static void Load3(string key) {
    var settings = new ES3Settings { memberReferenceMode = ES3.ReferenceMode.ByRef };
    var data = ES3.Load<SaveData>(key, settings);
    ES3.Load<List<GameObject>>(key + "objects", settings);
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Load(data);
  }
  static void ResetLiveData() {
    UnityEngine.Object.FindObjectsOfType<SaveObject>().ForEach(b => b.gameObject.Destroy());
  }
}