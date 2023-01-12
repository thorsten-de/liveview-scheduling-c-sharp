using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        private IList<IList<Task>> Columns = new List<IList<Task>>();

        private Queue<Task> ReadyTasks = new Queue<Task>();

        private void PrepareTasks()
        {
            Tasks.ForEach(t => t.Followers.Clear());
            foreach (var task in Tasks)
            {
                task.PrereqCount = task.PrereqTasks.Count;
                task.PrereqTasks.ForEach(pre => pre.AddFollower(task));
                task.IsCritical = false;
            }

            Tasks
              .Where(t => t.PrereqCount == 0)
              .ForEach(ReadyTasks.Enqueue);
        }

        private void EnqueueFollowers(Task task, Queue<Task> queue)
        {
            foreach (var follower in task.Followers)
            {
                follower.PrereqCount--;
                if (follower.PrereqCount == 0)
                    queue.Enqueue(follower);
            }
        }

        private void ProcessReadyTasks(IList<Task> into, Queue<Task> addFollowersTo)
        {
            while (ReadyTasks.Any())
            {
                var readyTask = ReadyTasks.Dequeue();
                readyTask.SetTimes();
                into.Add(readyTask);

                EnqueueFollowers(readyTask, addFollowersTo);
            }
        }

        public void TopoSort()
        {
            SortedTasks = new List<Task>(Tasks.Count);

            PrepareTasks();
            ProcessReadyTasks(into: SortedTasks, addFollowersTo: ReadyTasks);
        }

        public void BuildPertChart()
        {
            Columns.Clear();
            PrepareTasks();

            while (ReadyTasks.Any())
            {
                var newReadyTasks = new Queue<Task>();
                var newColumn = new List<Task>();
                Columns.Add(newColumn);

                ProcessReadyTasks(into: newColumn, addFollowersTo: newReadyTasks);
                ReadyTasks = newReadyTasks;
            }

            Task finalTask = Columns.Last().First();
            finalTask.MarkAsCritical();
        }

        public bool VerifySort() =>
          SortedTasks.All(appearAfterPrereq);

        private bool appearAfterPrereq(Task t)
        {
            var myIndex = SortedTasks.IndexOf(t);

            return t.PrereqTasks.All(pre => SortedTasks.IndexOf(pre) < myIndex);
        }

        private Task? ReadTask(StreamReader reader)
        {
            for (; ; )
            {
                var line = reader.ReadLine();
                if (line == null)
                    return null;

                var tokens = line.Split(",", 4, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length == 4)
                {
                    return new Task(
                      int.Parse(tokens[0]),
                      int.Parse(tokens[1]),
                      tokens[2],
                      tokens[3]
                        .Substring(1, tokens[3].Length - 2)
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

        public const double ItemWidth = 50;
        public const double ItemHeight = 75;
        public const double HorizontalGap = 40;
        public const double VerticalGap = 8;

        public static Brush Background = Brushes.LightBlue;
        public static Brush TaskBrush = Brushes.Black;
        public static double StrokeTickness = 1.0;

        private static double getVerticalCenter(Rect rect) => rect.Top + rect.Height / 2;

        public void DrawPertChart(Canvas canvas)
        {
            canvas.Children.Clear();

            Columns.Aggregate(0.0, (x, c) =>
            {
                c.Aggregate(0.0, (y, task) =>
                {
                    task.Bounds = new Rect(x, y, ItemWidth, ItemHeight);
                    return y + ItemHeight + VerticalGap;
                });
                return x + ItemWidth + HorizontalGap;
            });

            foreach (Task task in Tasks.Where(task => task.Bounds.Width > 0))
            {
                Point from = new(task.Bounds.Left, getVerticalCenter(task.Bounds));
                foreach (Task preTask in task.PrereqTasks)
                {
                    Point to = new(preTask.Bounds.Right, getVerticalCenter(preTask.Bounds));
                    canvas.DrawLine(from, to, Brushes.Gray, StrokeTickness);
                }
            }

            foreach (Task task in Tasks)
            {
                canvas.DrawRectangle(task.Bounds, Background, TaskBrush, StrokeTickness);
                string text = $"""
                    Task {task.Index}
                    Dur: {task.Duration}
                    Start: {task.StartTime}
                    End: {task.EndTime}
                    """;
                var label = canvas.DrawLabel(task.Bounds, text, Brushes.Transparent, TaskBrush, HorizontalAlignment.Center, VerticalAlignment.Center, 11, 2);
                label.ToolTip = task.Name;
            }
        }
    }
}
