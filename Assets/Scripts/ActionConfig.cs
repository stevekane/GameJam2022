using UnityEngine;

[CreateAssetMenu(fileName = "Action Config",menuName = "Actions/Config")]
public class ActionConfig : ScriptableObject {
  public int WindupFrames = 100;
  public int ActiveFrames = 1000;
  public int RecoveryFrames = 100;
}