using UnityEngine;

public class ResidualImageRenderer : MonoBehaviour {
  [SerializeField] SkinnedMeshRenderer SkinnedMeshRenderer;
  [SerializeField] string LayerName = "Visual";
  [SerializeField] Material Material;
  [Range(0,1)]
  public float Opacity = 1;
  public Color Color = Color.black;
  public float LifeTime = 1;

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