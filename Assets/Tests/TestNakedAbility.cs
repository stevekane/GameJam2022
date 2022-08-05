using System.Collections;
using UnityEngine;

public class TestNakedAbility : MonoBehaviour {
  public IEnumerator Any(IEnumerator a, IEnumerator b) {
    bool leftDone = false;
    bool rightDone = false;
    IEnumerator RunLeft(IEnumerator left) {
      yield return left;
      leftDone = true;
    }
    IEnumerator RunRight(IEnumerator right) {
      yield return right;
      rightDone = true;
    }
    var aFiber = StartCoroutine(RunLeft(a));
    var bFiber = StartCoroutine(RunRight(b));
    while (!leftDone & !rightDone) {
      yield return null;
    }
  }

  IEnumerator Fire(int index) {
    yield return new WaitForSeconds(.5f);
    Debug.Log($"Fire {index}");
  }

  IEnumerator Start() {
    Coroutine lastShot = null;
    for (var i = 0; i < 3; i++) {
      lastShot = StartCoroutine(Fire(i));
      yield return new WaitForSeconds(.2f);
    }
    yield return lastShot;
    Debug.Log("All shots complete");
  }

  IEnumerator WaitS(float s) {
    yield return new WaitForSeconds(s);
  }
}