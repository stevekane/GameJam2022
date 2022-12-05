using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core {
  public class Job {
    public JobContext Context;
    public Task Task;
    public Job(JobContext context, Task task) => (Context, Task) = (context, task);
    public void Cancel() => Context.Source.Cancel();
  }

  public class JobContext : IDisposable {
    public CancellationTokenSource Source = new();
    public void Dispose() => Source.Dispose();
    public Job Async<T>(Func<Task<T>> f) => new Job(this, Task.Run(f, Source.Token));
    public Job Run(Func<Task> f) => new Job(this, Task.Run(f, Source.Token));
    public Job Delay(int ms) => new Job(this, Task.Delay(ms, Source.Token));
  }

  public class Core : MonoBehaviour {
    JobContext MainContext = new();
    async void Start() {
      using var context = MainContext;
      var job = MainContext.Run(async delegate {
        try {
          Debug.Log("Start");
          await MainContext.Delay(5000).Task;
          Debug.Log("End");
        } catch (Exception e) {
          Debug.Log("Canceled");
        }
      });
      MainContext.Run(async delegate {
        await MainContext.Delay(2000).Task;
        job.Cancel();
      });
      await job.Task;
      Debug.Log($"{Time.time} seconds elapsed");
    }

    void OnDestroy() {
      if (!MainContext.Source.IsCancellationRequested) {
        MainContext.Source.Cancel();
      }
    }
  }
}