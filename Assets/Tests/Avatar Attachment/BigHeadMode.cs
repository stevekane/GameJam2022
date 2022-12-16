using UnityEngine;

public class BigHeadMode : MonoBehaviour {
  void Start() {
    GetComponent<AvatarTransform>().Transform.localScale = 2 * Vector3.one;
  }
}