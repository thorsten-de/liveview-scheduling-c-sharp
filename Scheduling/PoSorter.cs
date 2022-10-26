using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduling
{
  internal static class EnumerableExtensions
  {
    public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
    {
      foreach (var item in @this)
        action(item);
    }
  }

  internal class PoSorter
  {
    public IList<Task> Tasks { get; private set; } = new List<Task>();

    public IList<Task> SortedTasks { get; private set; } = new List<Task>();

    public void TopoSort()
    {
      foreach (var task in Tasks)
      {
        task.PrereqCount = task.PrereqTasks.Count;
        task.PrereqTasks.ForEach(pre => pre.AddFollower(task));
      }

      var sortedTasks = new List<Task>(Tasks.Count);
      var readyTasks = new Queue<Task>();

      Tasks
        .Where(t => t.PrereqCount == 0)
        .ForEach(readyTasks.Enqueue);

      while (readyTasks.Any())
      {
        var readyTask = readyTasks.Dequeue();
        sortedTasks.Add(readyTask);

        foreach (var follower in readyTask.Followers)
        {
          follower.PrereqCount--;
          if (follower.PrereqCount == 0)
            readyTasks.Enqueue(follower);
        }
      }

      SortedTasks = sortedTasks;
    }

    public bool VerifySort() =>
      SortedTasks.All(appearAfterPrereq);

    private static bool appearAfterPrereq(Task t) =>
      t.PrereqTasks.All(pre => pre.Index < t.Index);
  }
}
