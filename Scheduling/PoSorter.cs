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

    private bool appearAfterPrereq(Task t)
    {
      var myIndex = SortedTasks.IndexOf(t);
      if (myIndex == -1) 
        return false;

      return t.PrereqTasks.All(pre => SortedTasks.IndexOf(pre) < myIndex);
    }

    private Task? ReadTask(StreamReader reader)
    {
      for (; ; )
      {
        var line = reader.ReadLine();
        if (line == null)
          return null;

        var tokens = line.Split(",", 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 3)
        {
          return new Task(
            int.Parse(tokens[0]),
            tokens[1],
            tokens[2]
              .Substring(1, tokens[2].Length - 2)
              .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Select(int.Parse)
            );
        }
      }
    }

    public void LoadPoFile(string filename)
    {
      var tasks = new List<Task>();
      using (var reader = new StreamReader(filename))
      {
        for (; ; )
        {
          Task? t = ReadTask(reader);
          if (t == null) break;
          tasks.Add(t);
        }
      }
      
      tasks.ForEach(t => t.NumbersToTasks(tasks));
      Tasks = tasks;
    }
  }
}
