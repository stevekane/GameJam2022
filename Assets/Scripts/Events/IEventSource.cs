using System.Collections.Generic;

public interface IEventSource {
  public List<IEventSource> Connected { get; set; }
  public void Fire();
}

public interface IEventSource<T> {
  public List<IEventSource<T>> Connected { get; set; }
  public void Fire(T t);
}