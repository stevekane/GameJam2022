using System;

public interface IEventSource {
  public void Fire();
  public void Listen(Action handler);
  public void Unlisten(Action handler);
}

public interface IEventSource<T> {
  public void Fire(T t);
  public void Listen(Action<T> handler);
  public void Unlisten(Action<T> handler);
}

public class EventSource : IEventSource {
  Action Action;
  public void Fire() => Action?.Invoke();
  public void Listen(Action handler) => Action += handler;
  public void Unlisten(Action handler) => Action -= handler;
}

public class EventSource<T> : IEventSource<T> {
  Action<T> Action;
  public void Fire(T t) => Action?.Invoke(t);
  public void Listen(Action<T> handler) => Action += handler;
  public void Unlisten(Action<T> handler) => Action -= handler;
}