using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class ItemAmount {
  public ItemInfo Item;
  public int Count = 1;
}

[CreateAssetMenu(fileName = "Recipe", menuName = "Crafting/Recipe")]
public class Recipe : ScriptableObject {
  [FormerlySerializedAs("Time")]
  public float CraftTime;
  public ItemAmount[] Inputs;
  public ItemAmount[] Outputs;

  public bool Produces(ItemInfo item) => Outputs.Any(a => a.Item == item);
}