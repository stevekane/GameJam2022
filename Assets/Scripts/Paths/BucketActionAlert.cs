using UnityEngine;

public class BucketActionAlert : BucketAction {
  public override void Invoke(Bucket bucket) {
    Debug.Log(bucket.name + " reached the end of the path");
  }
}