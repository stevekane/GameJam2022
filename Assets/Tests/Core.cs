using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

#if DISABLED_DUE_TO_WARNINGS

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

Let's talk here about what we want to accomplish with a prototype.

There are few ideas rolling around in my head that seem to ask for a proof of concept:

  classes seem to provide several things:
    1. synchronous function-like recieve/send behaviors
    2. asynchronous processes that respond to various stimulii
    3. notion of referential identity
    4. guarantee that certain behaviors are observable to external messengers

  think of a class as a process which went sent an alert sends back a set of channels
  on which you can send it messages

    new alert
      | named Caller
        new inquirer
        new input
        alert <- inquirer
        inquirer -> method
        method <- input
        method -> output
        P(output)
      | named Class
        repeat
          new method
          new output
          alert -> other
          other <- method
          method -> input
          method <- output

  Let's perform the communication reductions by hand to see what happens to this system:

    alert <- inquirer | alert -> other
    ↓
    inquirer -> method | inquirer <- method
    ↓
    method <- input | method -> input
    ↓
    method -> output | method <- output
    ↓
    P(output) | Class

Another thing to understand, what is a mobile process?

  P | Q is a process

  (P|Q).R is also a process. P|Q are done when both P and Q are done

  (P.Q.R)|T is a process. P Q R are sequentially children of the unnamed process in parallel with T

  What does it mean to have a process that "spawns a child process outside its scope"?

  Let's imagine you have a channel that connects to some outer process.
  You send a token to that channel signaling it to spawn a process.
  That is literally all...

  νchannel
  νmandate
  νtimeout
  -- begin the outer process here
  | channel(token)
    P(token)
  | + mandate(notification)
      channel<token>
    + timeout(duration)

  In other words, there must exist some connection between the process capable of spawning
  the new process and the inner process that actually spawns the process.

  Details like higher-order process variables and whatever don't really matter here because
  you can imagine just passing a token which the recieving process understands to mean spawn
  this particular process (a sort of defunctionalization).

  So then, in general, the question is "Can we formulate in π-calculus an example system
  that is NOT structured in the sense of having access only to containing scopes?
  Let's elide the noise from creating channels for now.

  νchannel
  | channel<token>
  | channel(token).P
    ↓
  νchannel.P

  I think, after consideration, the problem here is that you have no guarantee that the process
  you have just asked to run will actually run. For example, you could have this:

  νc.c<t>

  You are hoping here that there exists some other process listening on c which will cause your
  desired process to start but there just isn't. You cannot KNOW that there isn't without essentially
  understanding the entire structure of the program. Structured concurrency makes a stronger guarantee
  by essentially ensuring that any scope you are given as a parameter must be available at the callsite
  meaning it is guaranteed to be some existing process that has not yet completed.

  One thing this does not allow, is for two concurrent processes to pass information between one
  another causing them to spawn processes.

  νc1
  νc2
  | repeat
      + c1<t1>.ch1(t2)
      + c2(t3).ch2<t4>
  | repeat
      + ...

  The essential point here, is that you can imagine having two concurrently-running processes that then
  exchange messages with one another causing each other to spawn processes. In fact, strictly speaking,
  in π-calculus, EVERY recieved message "spawns a process" in that it is defined as a continuation of the form
  c(t).P(t) where P(t) is some arbitrary process parameterized by the now-available t.

  You can run child processes in your scope with access to your scope.

  P_0 ≡ Q(0)_1 | R(0)_1
*/

/*
Task is a call-graph or syntax tree that may contain nested tasks.
Nested tasks come in two key varieties: Concurrent and Sequential.
P.Q

if P runs to completion then Q runs
if P is canceled then P.Q is canceled
if P.Q is canceled then either P or Q is canceled
if Q is canceled then P.Q is canceled

There is always exactly one child task active but it may itself
be a composite of sequential and parallel tasks! A failure from
this running child task is sent to the parent. A failure from the parent
is sent to its running child.

NOTE: The important thing here is ONLY that there is some kind of link
between any task and its context. The idea that the parent task somehow is
running the child task is just a mental model but is not actually part
of the programming model. These are ALL concurrent tasks executing but
the linkage that is created is a particular pattern that says that each
child task has additional listeners: Parent canceled/stopped and every
parent task has additional listeners: Child canceled/stopped.

You are free to define how this is handled however you so choose.

The fundamental thing we have is a context.
Context is linked to additional contexts.


KOTLIN JOB
New start Active
Active complete Completing
Active cancel Cancelling
Completing cancel Cancelling
Completing finish Completed
Cancelling finish Cancelled

eventually we send complete/cancel on result

νcancel
νresult
νcancelP
νresultP
νcancelQ
νresultP
| + P
    resultP<complete>
  + cancelP(_)
    resultP<canceled>
  + resultP<canceled>
| + Q
    resultQ<complete>
  + cancelQ(_)
    resultQ<canceled>
  + resultQ<canceled>
| cancel(_)
  | cancelP<_>
  | cancelQ<_>
  ∧ resultP(_)
  ∧ resultQ(_)
  Cleanup
  result<canceled>
| + ∧ resultP(completed)
    ∧ resultQ(completed)
    result<completed>
  + ∧ resultP(canceled)
    ∧ resultQ(_)
    result<canceled>
  + ∧ resultP(_)
    ∧ resultQ(canceled)
    result<canceled>

Challenge: Is it possible to "kill" a running process from the outside?

  P|kill(P) ↓ 0

What about a lesser challenge: define a system in which a process may be killed
by itself.

  P.0

Define a process that can kill itself with one of two names sent over a channel:

  νc.
  | c(status).if status = complete then P else Q
  | c<complete> + c<canceled>

Define a system that terminates when such termination is requested from another process:

  νc.
  | !P + c(kill)
  | c<kill>

Define a system that takes an arbitrary process P and runs it alongside a process:

  CancellableProcess(c,P) ≡ c(stop) + P

This is an ATOM of the context of cancellable tasks.

If P itself contains multiple suspension points, every one of them must be placed in this atom:

Let P ≡ Wait(30).Q|P.channel(stop)
Let ? ≡ CancellableProcess(c)

To make P fully cancellable, we need to turn it into this:

?P ≡
  ?Wait(3)
  ?(?Q|?P)
  ?channel(stop)

Task<C> ≡
  Task<A> >>= λ(a:A).Task<B> >>= λ(b:B).Task<T>

Task<C> ≡ do
  a <- Task<A>
  b <- Task<B>(a)
  Task<C>(b)

Task<(A,B)> ≡ do
  (a,b) -> Task<A> <*> Task<B>
  pure (a,b)

This all feels mostly correct except the details seem murky.

a context defines the ability to run concurrent routines
a context is done when all of its child routines are done

a context is a job? if this is true, it would be one that supports two operations:
  await aka sequential composition
  run aka concurrent composition

contexts are the only things that can run jobs
jobs can
*/

static class ContextExtensions {
  public static async Task Canceled() => await Task.Yield();
  public static Task Launch(this Context ctx, Func<Task> continuation) {
    if (!ctx.TokenSource.Token.IsCancellationRequested) {
      var job = new Job { Task = continuation() };
      ctx.Jobs.Add(job);
      return job.Task;
    } else {
      return Canceled();
    }
  }
}

class Context : IAsyncDisposable {
  public List<Job> Jobs = new();
  public CancellationTokenSource TokenSource = new CancellationTokenSource();
  ~Context() => TokenSource?.Dispose();
  public async ValueTask DisposeAsync() => await Task.WhenAll(Jobs.Select(j => j.Task));
}

class Job {
  public Task Task;
}

public class Core : MonoBehaviour {
  CancellationTokenSource TokenSource = new();

  async Task Chatter() {
    await using var ctx = new Context();
    await ctx.Launch(() => Task.Delay(1000));
    Debug.Log("Chatter");
  }

  async Task Thunk() {
    await Task.Yield();
    Debug.Log("Thunk");
  }

  async void Start() {
    await using var ctx = new Context { TokenSource = TokenSource };
    ctx.Launch(async () => {
      while (!ctx.TokenSource.IsCancellationRequested) {
        await ctx.Launch(Chatter);
      }
    });
    ctx.Launch(async () => {
      await Task.Delay(1000);
      Debug.Log("Ping");
    });
    ctx.Launch(async () => {
      await Task.Delay(2000);
      Debug.Log("Pong");
    });
    ctx.Launch(async () => {
      await Task.Delay(3000);
      ctx.TokenSource.Cancel();
    });
  }

  // async void Start () {
  //   async Task<string> Ping() {
  //     await Task.Delay(1000);
  //     return "ping";
  //   }
  //   async Task<string> Pong() {
  //     await Task.Delay(2000);
  //     return "pong";
  //   }
  //   async Task<int> Num() {
  //     await Task.Yield();
  //     return 1;
  //   }
  //   var stuff = await Task.WhenAll(Ping(), Pong());
  //   Debug.Log(stuff[0]);
  //   Debug.Log(stuff[1]);
  //   var ping = Ping();
  //   var num = Num();
  //   await Task.WhenAll(ping, num);
  //   Debug.Log(ping.Result);
  //   Debug.Log(num.Result);
  // }

  void OnDestroy() => TokenSource.Cancel();
}

#endif