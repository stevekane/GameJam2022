using UnityEngine;

namespace Archero {
  public class Bolt : MonoBehaviour {
    LineRenderer LineRenderer;
    Transform Source;
    Transform Target;

    public static void Create(Bolt prefab, Transform source, Mob target) {
      var bolt = Instantiate(prefab, source.position, Quaternion.identity);
      bolt.Source = source;
      bolt.Target = target.transform;
      bolt.LineRenderer.SetPosition(0, source.position + Vector3.up);
      bolt.LineRenderer.SetPosition(1, target.transform.position + Vector3.up);
    }

    void Awake() {
      this.InitComponent(out LineRenderer);
    }

    void FixedUpdate() {
      if (Source)
        LineRenderer.SetPosition(0, Source.position + Vector3.up);
      if (Target)
        LineRenderer.SetPosition(1, Target.transform.position + Vector3.up);
    }
  }
}