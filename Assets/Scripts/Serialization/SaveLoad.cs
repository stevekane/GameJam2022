using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public interface ISaveableObject {
  public ILoadableObject Save();
}

public interface ILoadableObject {
  public void Load();
}

// Represents all the save data we read/write to the save file.
public class SaveData {
  public List<ILoadableObject> Entities;
  public List<UpgradeData> Upgrades;
  public int Gold;

  static string Key(int slot) => $"game{slot}";

  // TODO: use async IO
  public static void SaveToFile(int slot) {
    try {
      Save3(Key(slot));
      Debug.Log($"Saved");
    } catch (Exception e) {
      Debug.Log($"Save failed: {e}");
    }
  }
  public static void LoadFromFile(int slot) {
    try {
      ResetLiveData();
      Load3(Key(slot));
    } catch (Exception e) {
      Debug.Log($"Load failed: {e}");
    }
  }

  static void Save3(string key) {
    SaveData data = new();
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Save(data);
    data.Entities = UnityEngine.Object.FindObjectsOfType<SaveObject>().Select(b => b.Save()).ToList();
    var settings = new ES3Settings { memberReferenceMode = ES3.ReferenceMode.ByRef };
    ES3.Save(key, data, settings);
  }
  static void Load3(string key) {
    var settings = new ES3Settings { memberReferenceMode = ES3.ReferenceMode.ByRef };
    var data = ES3.Load<SaveData>(key, settings);
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Load(data);
    data.Entities.ForEach(b => b.Load());
  }
  static void ResetLiveData() {
    UnityEngine.Object.FindObjectsOfType<SaveObject>().ForEach(b => b.gameObject.Destroy());
  }
}