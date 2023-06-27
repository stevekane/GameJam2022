using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archero {
  public class GameManager : MonoBehaviour {
    public static GameManager Instance;
    
    public List<SceneAsset> Scenes;
    public int CurrentLevel => Scenes.FindIndex(s => s.name == SceneManager.GetActiveScene().name);

    void Awake() {
      Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
      if (Instance) {
        Destroy(gameObject);
      } else {
        Instance = this;
        //this.InitComponentFromChildren(out NavMeshUtil.Instance);
        DontDestroyOnLoad(Instance.gameObject);
      }
    }

    public void OnLevelComplete() {
      if (CurrentLevel+1 < Scenes.Count) {
        var next = Scenes[CurrentLevel+1];
        SceneManager.LoadSceneAsync(next.name);
      } else {
        Debug.Log($"Victory!");
      }
    }
  }
}