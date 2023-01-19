using UnityEngine;

public class ResidualImageRenderer : MonoBehaviour {
  [SerializeField] SkinnedMeshRenderer SkinnedMeshRenderer;
  [SerializeField] Material Material;
  [SerializeField, Range(0,1)] float Opacity = 1;
  [SerializeField] Color Color = Color.black;
  [SerializeField] float LifeTime = 1;
  [SerializeField] string LayerName = "Visual";

  public void Render() {
    var mesh = new Mesh();
    var image = new GameObject("Residual Image");
    var meshRenderer = image.AddComponent<MeshRenderer>();
    var meshFilter = image.AddComponent<MeshFilter>();
    SkinnedMeshRenderer.BakeMesh(mesh);
    meshFilter.mesh = mesh;
    meshRenderer.material = Material;
    meshRenderer.material.SetColor("_Color", Color);
    meshRenderer.material.SetFloat("_Opacity", Opacity);
    meshRenderer.material.SetFloat("_StartTime", Time.time);
    meshRenderer.material.SetFloat("_EndTime", Time.time + LifeTime);
    image.layer = LayerMask.NameToLayer(LayerName);
    image.transform.SetPositionAndRotation(transform.position, transform.rotation);
    image.transform.localScale = transform.localScale;
    Destroy(image, LifeTime);
  }
}