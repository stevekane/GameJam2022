using UnityEngine;

namespace Archero {
  public class Bolt : MonoBehaviour {
    LineRenderer LineRenderer;

    public static void Create(Bolt prefab, Transform source, Mob target) {
      var bolt = Instantiate(prefab, source.position, Quaternion.identity);
      bolt.LineRenderer.SetPosition(0, source.position + Vector3.up);
      bolt.LineRenderer.SetPosition(1, target.transform.position + Vector3.up);
    }

    void Awake() {
      this.InitComponent(out LineRenderer);
    }
  }
}