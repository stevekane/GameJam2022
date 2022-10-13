using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableObjectConverter : JsonConverter {
  class FuckingUnityScriptableObjectWrapper {
    public ScriptableObject Object;
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
    var s = JsonUtility.ToJson(new FuckingUnityScriptableObjectWrapper() { Object = (ScriptableObject)value });
    Debug.Log($"Newton writing: {s}");
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
    var tt = objectType.IsSubclassOf(typeof(ScriptableObject));
    if (tt)
      Debug.Log($"Newton subclass type: {objectType}");
    return tt;
  }
}

public class SaveData {
  public List<UpgradeData> Upgrades;

  static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() {
    Converters = new List<JsonConverter>() { new ScriptableObjectConverter() },
    TypeNameHandling = TypeNameHandling.Auto,
  };
  public static string Save() {
    SaveData data = new();
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Save(data);
    return JsonConvert.SerializeObject(data, Formatting.Indented, JsonSerializerSettings);
  }
  public static void Load(string json) {
    var data = JsonConvert.DeserializeObject<SaveData>(json, JsonSerializerSettings);
    UnityEngine.Object.FindObjectOfType<Player>().GetComponent<Upgrades>().Load(data);
  }
}