using System.Linq;
using UnityEngine;

public class SampleActionConditioned : SampleAction {
  [SerializeField] Condition[] Conditions;

  public override bool Satisfied => Conditions.All(c => c.Satisfied);
}