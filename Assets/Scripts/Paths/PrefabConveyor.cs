using UnityEngine;
using UnityEditor;

public class PrefabConveyor : MonoBehaviour {
  [SerializeField]  
  Mob MobPrefab;
  [SerializeField]
  Bucket BucketPrefab;
  [SerializeField]
  Transform DynamicContentRoot;
  [SerializeField]
  [Range(0,64)]
  int Count;

  T GetOrCreateComponent<T>(GameObject go) where T : MonoBehaviour {
    if (go.TryGetComponent(out T t)) {
      return t;
    } else {
      return go.AddComponent<T>();
    }
  }

  public void Run() {
    if (DynamicContentRoot == transform) {
      Debug.LogError("Do NOT use transform as dynamic content root. Create a child object.");
      return;
    }
    if (!DynamicContentRoot) {
      Debug.LogError("Must assign dynamic content root");
      return;
    }
    if (!BucketPrefab) {
      Debug.LogError("Must assign Bucket Prefab");
      return;
    }
    if (!MobPrefab) {
      Debug.LogError("Must assign mob prefab");
      return;
    }

    var conveyor = GetComponent<Conveyor>();
    while (DynamicContentRoot.childCount > 0) {
      DestroyImmediate(DynamicContentRoot.GetChild(0).gameObject);
    }
    conveyor.Buckets.Clear();
    for (int i = 0; i < Count; i++) {
      var bucket = PrefabUtility.InstantiatePrefab(BucketPrefab) as Bucket;
      var mob = PrefabUtility.InstantiatePrefab(MobPrefab) as Mob;
      mob = mob ? mob : Instantiate(MobPrefab);
      var patrol = GetOrCreateComponent<MobMovePatrol>(mob.gameObject);
      var distance = (float)i/(float)Count;
      var pathdata = conveyor.Path.ToWorldSpace(distance);
      patrol.Target = bucket.transform;
      mob.transform.SetParent(DynamicContentRoot,false);
      mob.transform.SetPositionAndRotation(pathdata.Position,pathdata.Rotation);
      bucket.Distance = distance;
      bucket.transform.SetParent(DynamicContentRoot,false);
      bucket.transform.SetPositionAndRotation(pathdata.Position,pathdata.Rotation);
      conveyor.Buckets.Add(bucket);
    }
  }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(PrefabConveyor))]
public class PrefabConveyorEditor : Editor {
  public override void OnInspectorGUI() {
    PrefabConveyor prefabConveyor = (PrefabConveyor)target;
    EditorGUI.BeginChangeCheck();
    DrawDefaultInspector();
    if (EditorGUI.EndChangeCheck()) {
      prefabConveyor.Run();
    }
  }
}