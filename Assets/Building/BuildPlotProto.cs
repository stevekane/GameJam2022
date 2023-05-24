using UnityEngine;

[CreateAssetMenu(fileName = "BuildPlot", menuName = "Crafting/BuildPlotProto")]
public class BuildPlotProto : ItemProto {
  public override void OnCrafted(Crafter crafter) {
    Spawn(crafter.transform.position, crafter.transform.rotation);
    crafter.gameObject.Destroy();
  }
}