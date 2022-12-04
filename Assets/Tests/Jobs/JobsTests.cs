using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Suspending functions are state machines which react to an event, do something, then
yield a value to their owning state machine.

Let us use a task-like syntax to illustrate the equivalence:

Task f(bool flipflop)
  await g()
  print flipflop
  f(!flipflop)

Task g()
  sleep(10)

f is infinitely recursive so we expect the following behavior from f(true)
  repeat
    sleep(10)
    print(true)
    sleep(10)
    print(false)

f can also be thought of as a function/state pair:

Status : Running + Complete

Fstate : Running × bool × GState?
ffunc (Running flipflop Complete) =
  print flipflop
  Running * ¬flipflop * null
ffunc (flipflop * gState) =
  gState

Gstate = Status
gfunc gs = sleep(10)
*/

public class JobsTests : MonoBehaviour {
}