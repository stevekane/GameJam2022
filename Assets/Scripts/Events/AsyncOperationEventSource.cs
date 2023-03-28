using System;
using UnityEngine;

public class AsyncOperationEventSource : IEventSource<AsyncOperation> {
  public AsyncOperation Operation;
  public AsyncOperationEventSource(AsyncOperation operation) => Operation = operation;
  public void Listen(Action<AsyncOperation> handler) => Operation.completed += handler;
  public void Set(Action<AsyncOperation> handler) => throw new NotSupportedException("AsyncOperation set not supported");
  public void Unlisten(Action<AsyncOperation> handler) => Operation.completed -= handler;
  public void Clear() => throw new NotSupportedException("AsyncOperation clear not supported");
  public void Fire(AsyncOperation op) {}
}