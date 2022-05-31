using UnityEngine;

public class RunaroundManager : MonoBehaviour {
  public SceneManager SceneManager;

  public void Start() {
    Debug.Log(SceneManager);
    Debug.Log(SceneManager.Boot);
  }
}