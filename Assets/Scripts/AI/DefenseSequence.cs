using UnityEngine;

public class DefenseSequence : MonoBehaviour {
  public enum TargetChoiceType { BehindTarget, AwayFromTarget };
  public int Score;
  public Ability Ability;
  public TargetChoiceType TargetChoice;
}