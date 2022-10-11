using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
The core of the game.

The idea here is to identify the small model of what is happening in our larger game.
Namely, it is to discover the core elements that make up a characters ability to
perform actions and to affect the world.

A character has a set of abilities.
The set of abilities constitutes its possible actions.
There is some abstract process that decides what possible action (including none) to take.
  A player observes the game with their senses and makes their desires known through inputs.
  An AI observes the game through query of the game state and makes their desires known through code.
An entity is affected by two things:
  Its own intentions
  Interactions with its environment
Actions may require preconditions to be met before they can be taken.
Actions may require ongoing conditions that if unsatisfied stop the action.
We wish NOT to enforce any particular ordering of concurrent events.
This means that we must have events affect entities through one of two possible systems:
  Normalizable or orderable actions that are normalized then processed every frame
  Commutative and associative operations

Given a set of actions A and a commutative and associative binary operation + we can then say that:

  ∀a,b ∈ A, a + b = b + a.

Alternatively, we can say that given a set of actions A and a normalization operation ↓ : [A] → [A]
we can then say that:

  ∀a ∈ [A], ∀b ∈ permutations(a), a↓ = b↓

In a world of message-passing, we therefore seek to constrain the nature of a process
listening to some kinds of messages rather than to constrain something about the behavior
of the processes themselves. That is to say, a process P might send message M on channel C
which is listened to by some other process Q. When Q recieves M it should be capable of handling
the message such that the order messages arrive does not matter. This is extremely informal
but we shall try here to elaborate on this constraint and try to make it more specific.

Let us say declare a process P which listens for an integer on channel C:

  P ≡ C(x:Int)...

This process should also continuously broadcast its "current value" on some channel B:

  P(b:Int) ≡ C(x:Int).P(b+x) + D<v>.P(b)

Finally, the process should be able to be reset to a base value by reciving a message on R:

  P(n) ≡
    + C(x).P(n+x)
    + D<n>.P(n)
    + R(b).P(b)

The property we would like to hold is that no matter what order we send messages to P(n)

  P(b) | C<n>.C<m>.D(t) -> P(b+n) | C<m>.D(t) -> P(b+n+m) | D(t) -> P(b+n+m)
  P(b) | C<m>.C<n>.D(t) -> P(b+m) | C<n>.D(t) -> P(b+m+n) | D(t) -> P(b+m+n)

By commutativity of addition of integers we can say that these two systems normalize
to equal processes.

Every entity in the system is thus a state of disjoint commutative properties such that
s_n
*/
public class Core : MonoBehaviour {
  void Start() {

  }

  void Update() {

  }
}