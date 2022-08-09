using System.Collections;
using static Fiber;

public class WindRun : Ability {
  public Timeval Duration = Timeval.FromMillis(3000)  ;

  protected override IEnumerator MakeRoutine() {
    yield return Wait(Duration.Frames);
    Stop();
  }
}