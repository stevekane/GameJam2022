using UnityEngine;
using UnityEngine.Serialization;

public class SimpleTags : MonoBehaviour {
  public AbilityTag CancelAbilitiesWith;
  public AbilityTag BlockAbilitiesWith;
  public AbilityTag OwnerActivationRequired;
  public AbilityTag OwnerActivationBlocked;
  [FormerlySerializedAs("OwnerWhileActive")]
  public AbilityTag OwnerOnRun;
  [FormerlySerializedAs("OnStart")]
  public AbilityTag AbilityOnRun;
}