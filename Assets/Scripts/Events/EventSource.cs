using System;

public interface IEventSource {
  public int Priority { get; }
  public void Listen(Action handler, int priority = 0);
  public void Unlisten(Action handler);
  public void Fire();
}

public interface IEventSource<T> {
  public int Priority { get; }
  public void Listen(Action<T> handler, int priority = 0);
  public void Unlisten(Action<T> handler);
  public void Fire(T t);
}

public class EventSource : IEventSource {
  public Action Action;
  public int Priority { get; private set; }
  public void Listen(Action handler, int priority = 0) {
    if (priority >= Priority) {
      Priority = priority;
      Action = handler;
    }
  }
  public void Unlisten(Action handler) => Action -= handler;
  public void Fire() => Action?.Invoke();
}

public class EventSource<T> : IEventSource<T> {
  public Action<T> Action;
  public int Priority { get; private set; }
  public void Listen(Action<T> handler, int priority = 0) {
    if (priority >= Priority) {
      Priority = priority;
      Action = handler;
    }
  }
  public void Unlisten(Action<T> handler) => Action -= handler;
  public void Fire(T t) => Action?.Invoke(t);
}

class ScopedListener : IDisposable {
  IEventSource Event;
  Action Action;
  public ScopedListener(IEventSource evt, Action action) {
    (Event, Action) = (evt, action);
    Event.Listen(Action);
  }
  public void Dispose() => Event.Unlisten(Action);
}
class ScopedListener<T> : IDisposable {
  IEventSource<T> Event;
  Action<T> Action;
  public ScopedListener(IEventSource<T> evt, Action<T> action) {
    (Event, Action) = (evt, action);
    Event.Listen(Action);
  }
  public void Dispose() => Event.Unlisten(Action);
}