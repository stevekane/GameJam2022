using System;

public interface IEventSource {
  public void Listen(Action handler);
  public void Unlisten(Action handler);
  public void Set(Action handler);
  public void Clear();
  public void Fire();
}

public interface IEventSource<T> {
  public void Listen(Action<T> handler);
  public void Unlisten(Action<T> handler);
  public void Set(Action<T> handler);
  public void Clear();
  public void Fire(T t);
}

public class EventSource : IEventSource {
  public Action Action;
  public void Listen(Action handler) => Action += handler;
  public void Set(Action handler) => Action = handler;
  public void Unlisten(Action handler) => Action -= handler;
  public void Clear() => Action = default;
  public void Fire() => Action?.Invoke();
}

public class EventSource<T> : IEventSource<T> {
  public Action<T> Action;
  public void Listen(Action<T> handler) => Action += handler;
  public void Set(Action<T> handler) => Action = handler;
  public void Unlisten(Action<T> handler) => Action -= handler;
  public void Clear() => Action = default;
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