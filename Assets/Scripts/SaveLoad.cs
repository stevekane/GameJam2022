using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

// Represents all the save data we read/write to the save file.
public class SaveData {
  public List<UpgradeData> Upgrades;
  public int Gold;

  static string FilePath { get => System.IO.Path.Combine(Application.persistentDataPath, "save.json"); }
  static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() {
    Converters = new List<JsonConverter>() { new ScriptableObjectConverter() },
    TypeNameHandling = TypeNameHandling.Auto,
  };

  // TODO: use async IO
  public static void SaveToFile() {
    var json = SaveToJson();
    File.WriteAllTextAsync(FilePath, json);
    Debug.Log($"Saved to ${FilePath}");
  }
  public static void LoadFromFile() {
    if (!File.Exists(FilePath))
      return;
    var json = File.ReadAllText(FilePath);
    LoadFromJson(json);
  }

  static string SaveToJson() {
    SaveData data = new();
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Save(data);
    return JsonConvert.SerializeObject(data, Formatting.Indented, JsonSerializerSettings);
  }
  static void LoadFromJson(string json) {
    var data = JsonConvert.DeserializeObject<SaveData>(json, JsonSerializerSettings);
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Load(data);
  }
}