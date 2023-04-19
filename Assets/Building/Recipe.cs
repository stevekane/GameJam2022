using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Recipe", menuName = "Crafting/Recipe")]
public class Recipe : ScriptableObject {
  [Serializable]
  public class ItemAmount {
    public ItemInfo Item;
    public int Count = 1;
  }

  [FormerlySerializedAs("Time")]
  public float CraftTime;
  public ItemAmount[] Inputs;
  public ItemAmount[] Outputs;

  public bool Produces(ItemInfo item) => Outputs.Any(a => a.Item == item);
}