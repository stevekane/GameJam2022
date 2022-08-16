using System;

public interface IEventSource {
  public void Listen(Action handler);
  public void Unlisten(Action handler);
  public void Fire();
}

public interface IEventSource<T> {
  public void Listen(Action<T> handler);
  public void Unlisten(Action<T> handler);
  public void Fire(T t);
}

public class EventSource : IEventSource {
  Action Action;
  public void Listen(Action handler) => Action += handler;
  public void Unlisten(Action handler) => Action -= handler;
  public void Fire() => Action?.Invoke();
}

public class EventSource<T> : IEventSource<T> {
  Action<T> Action;
  public void Listen(Action<T> handler) => Action += handler;
  public void Unlisten(Action<T> handler) => Action -= handler;
  public void Fire(T t) => Action?.Invoke(t);
}