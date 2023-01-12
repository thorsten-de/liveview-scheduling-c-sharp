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

        public const double ItemWidth = 48;
        public const double ItemHeight = 64;
        public const double HorizontalGap = 32;
        public const double VerticalGap = 8;

        public const double StrokeThickness = 1;


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

            DrawLinks(canvas);

            DrawTasks(canvas);
        }

        private void DrawTasks(Canvas canvas)
        {
            foreach (Task task in Tasks)
            {
                (Brush background, Brush foreground) = task.IsCritical
                    ? (Brushes.Pink, Brushes.Red)
                    : (Brushes.LightBlue, Brushes.Black);

                canvas.DrawRectangle(task.Bounds, background, foreground, StrokeThickness);

                string text = $"""
                    Task {task.Index}
                    Dur: {task.Duration}
                    Start: {task.StartTime}
                    End: {task.EndTime}
                    """;
                var label = canvas.DrawLabel(task.Bounds, text, Brushes.Transparent, foreground, HorizontalAlignment.Center, VerticalAlignment.Center, 11, 2);
                label.ToolTip = task.Name;
            }
        }

        private void DrawLinks(Canvas canvas)
        {
            foreach (Task task in Tasks.Where(task => task.Bounds.Width > 0))
            {
                Point from = new(task.Bounds.Left, getVerticalCenter(task.Bounds));
                foreach (Task preTask in task.PrereqTasks)
                {
                    Brush color = Brushes.Gray;
                    double thickness = StrokeThickness;
                    if (task.IsCriticalDependentOn(preTask))
                    {
                        thickness = 3.0;
                        if (task.IsCritical)
                        {
                            color = Brushes.Red;
                        }

                    }
                    Point to = new(preTask.Bounds.Right, getVerticalCenter(preTask.Bounds));
                    canvas.DrawLine(from, to, color, thickness);
                }
            }
        }

        public static Size CellSize = new Size(32, 32);
        public static double RowHeaderWidth = 128;
        public static Brush GridColor = Brushes.LightGray;
        public static double GridThickness = 1.0;
        public static Brush ColumnHeaderColor = Brushes.ForestGreen;
        public static Brush RowHeaderColor = Brushes.Black;
        public static double LinkSpacing = 5.0;


        public void DrawGrid(Canvas canvas, int dateColumns)
        {
            var top = 0;
            double left = RowHeaderWidth;
            var bottom = (Tasks.Count + 1) * CellSize.Height + top;
            double x = left;
            for (int i = 0; i <= dateColumns; i++) {
                canvas.DrawLine(new Point(x, top), new Point(x, bottom), GridColor, GridThickness);
                x += CellSize.Width;
            }

            double right = left + dateColumns * CellSize.Width;
            double y = top;
            for (int i = 0; i < Tasks.Count + 2; i++)
            {
                canvas.DrawLine(new Point(left, y), new Point(right,  y), GridColor, GridThickness);
                y += CellSize.Height;
            }
        }

        public void DrawColumnHeaders(Canvas canvas, int start, int finish) 
        {
            var top = 0;
            double left = RowHeaderWidth;
            for (int day = start; day <= finish; day++)
            {
                canvas.DrawLabel(new Rect(new Point(left, top), CellSize), day, Brushes.Transparent, ColumnHeaderColor, HorizontalAlignment.Center, VerticalAlignment.Center, 11, 1);
                left += CellSize.Width;
            }
        }


        public void DrawRowHeaders(Canvas canvas) {
            double left = 0;
            Tasks.Aggregate(CellSize.Height, (y, task) =>
            {
                canvas.DrawLabel(new Rect(left, y, RowHeaderWidth, CellSize.Height), $"{task.Index}. {task.Name}", Brushes.Transparent, RowHeaderColor, HorizontalAlignment.Left, VerticalAlignment.Center, 11, 1);
                return y + CellSize.Height;
            });

        }

        private double DayToX(int day) => day * CellSize.Width;

        private void ArrangeTasks()
        {
            double top = CellSize.Height;
            Tasks.Aggregate(top + CellSize.Height / 4.0, (y, task) =>
            {
                task.Bounds = new Rect(RowHeaderWidth + DayToX(task.StartTime), y, DayToX(task.Duration), CellSize.Height / 2);
                return y + CellSize.Height;
            });
        }

        public void DrawGanttChart(Canvas canvas)
        {
            canvas.Children.Clear();

            var minDay = Tasks.Min(t => t.StartTime);
            var maxDay = Tasks.Max(t => t.EndTime);

            DrawGrid(canvas, maxDay - minDay);
            DrawColumnHeaders(canvas, minDay+1, maxDay);
            DrawRowHeaders(canvas);
            
            ArrangeTasks();

            foreach (Task task in Tasks)
            {
                task.PrereqTasks.Aggregate(task.Bounds.Left + LinkSpacing, (x, preTask) =>
                {
                    Brush color = Brushes.Gray;
                    double thickness = StrokeThickness;
                    if (task.IsCriticalDependentOn(preTask))
                    {
                        thickness = 3.0;
                        if (task.IsCritical)
                        {
                            color = Brushes.Red;
                        }
                    }
                    
                    Point from = new(preTask.Bounds.Right, getVerticalCenter(preTask.Bounds));
                    Point to = new(x, from.Y > task.Bounds.Bottom ? task.Bounds.Bottom : task.Bounds.Top);
                    Point corner = new Point(to.X, from.Y);
                    canvas.DrawLine(from, corner, color, thickness);
                    canvas.DrawLine(corner, to, color, thickness);
                    return x + LinkSpacing;
                });
            }
            
            foreach (Task task in Tasks)
            {
                (Brush background, Brush foreground) = task.IsCritical
                    ? (Brushes.Pink, Brushes.Red)
                    : (Brushes.LightBlue, Brushes.Blue);
                
                var taskRect = canvas.DrawRectangle(task.Bounds, background, foreground, StrokeThickness);
                taskRect.ToolTip = task.Name;
            }
            
        }
    }
}
