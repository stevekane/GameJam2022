using UnityEngine;

[CreateAssetMenu(fileName = "BuildPlot", menuName = "Crafting/BuildPlotProto")]
public class BuildPlotProto : ItemProto {
  public BuildObject BuildObject;
  public override void OnCrafted(Crafter crafter) {
    Instantiate(BuildObject, crafter.transform.position, crafter.transform.rotation);
    crafter.gameObject.Destroy();
  }
}