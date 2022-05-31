using UnityEngine;

// Create instance of Application-wide Boot object if not found
public class SceneManager : MonoBehaviour {
  [HideInInspector]
  public Boot Boot;
  [SerializeField]
  private Boot BootPrefab;

  public void Awake() {
    var includeInactive = false;
    var boot = GameObject.FindObjectOfType<Boot>(includeInactive);
    if (boot) {
      Boot = boot;
    } else {
      Boot = Instantiate(BootPrefab,transform);
    }
  }

  public void OnDestroy() {
    Destroy(Boot);
    Boot = null;
  }
}