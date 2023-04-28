using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// A Newtsonsoft JSON converter that writes ScriptableObjects the Unity way.
// Contains a hack to work around weird quirks in Unity's Json de/serializer.
public class ScriptableObjectConverter : JsonConverter {
  class FuckingUnityScriptableObjectWrapper {
    public ScriptableObject Object;
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
    var s = JsonUtility.ToJson(new FuckingUnityScriptableObjectWrapper() { Object = (ScriptableObject)value });
    writer.WriteValue(s);
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
    var wrapper = JsonUtility.FromJson<FuckingUnityScriptableObjectWrapper>((string)reader.Value);
    return wrapper.Object;
  }

  public override bool CanRead {
    get { return true; }
  }

  public override bool CanConvert(Type objectType) {
    return objectType.IsSubclassOf(typeof(ScriptableObject)); ;
  }
}

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

  static string FilePath(int slot) => System.IO.Path.Combine(Application.persistentDataPath, $"save{slot}.json");
  static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() {
    Converters = new List<JsonConverter>() { new ScriptableObjectConverter(), new Vector3Converter(), new QuaternionConverter() },
    TypeNameHandling = TypeNameHandling.Auto,
  };

  // TODO: use async IO
  public static void SaveToFile(int slot) {
    try {
      var json = SaveToJson();
      Debug.Log($"Save {json}");
      File.WriteAllTextAsync(FilePath(slot), json);
    } catch (Exception e) {
      Debug.Log($"Save failed: {e}");
    }
  }
  public static void LoadFromFile(int slot) {
    try {
      ResetLiveData();
      if (!File.Exists(FilePath(slot)))
        return;
      var json = File.ReadAllText(FilePath(slot));
      LoadFromJson(json);
    } catch (Exception e) {
      Debug.Log($"Load failed: {e}");
    }
  }

  static string SaveToJson() {
    SaveData data = new();
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Save(data);
    data.Entities = UnityEngine.Object.FindObjectsOfType<SaveObject>().Select(b => b.Save()).ToList();
    return JsonConvert.SerializeObject(data, Formatting.Indented, JsonSerializerSettings);
  }
  static void LoadFromJson(string json) {
    var data = JsonConvert.DeserializeObject<SaveData>(json, JsonSerializerSettings);
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Load(data);
    data.Entities.ForEach(b => b.Load());
  }
  static void ResetLiveData() {
    UnityEngine.Object.FindObjectsOfType<SaveObject>().ForEach(b => b.gameObject.Destroy());
  }
}