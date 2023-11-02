using UnityEngine;

public abstract class SingletonBehavior<T> : MonoBehaviour where T : MonoBehaviour {
  public static T Instance;

  void Awake() {
    if (Instance) {
      Destroy(gameObject);
    } else {
      Instance = this as T;
      transform.SetParent(null);
      DontDestroyOnLoad(gameObject);
      AwakeSingleton();
    }
  }

  protected virtual void AwakeSingleton() { }
}