using System.Collections.Generic;
using UnityEngine;

public class MaterialTester : MonoBehaviour {
  [ColorUsage(true,true)]
  [SerializeField] Color Color;
  [SerializeField] int Index = 1;
  [SerializeField] string Name = "_EmissionColor";
  [SerializeField] MeshRenderer MeshRenderer;

  List<Material> Materials = new();

  void Update() {
    MeshRenderer.GetMaterials(Materials);
    Materials[Index].SetVector(Name, Color);
  }
}