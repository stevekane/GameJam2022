using UnityEngine;

// Identifier for which team a character is on - e.g., player or mob.
public class Team : MonoBehaviour {
  public int ID;

  public bool CanBeHurtBy(int otherID) => ID != otherID;
  public bool CanBeHurtBy(Team other) => ID != other.ID;
}