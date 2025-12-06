using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Scheduling
{
    internal class Task
    {
        public int Index { get; private set; }
        public string Name { get; private set; }
        
        public int Duration { get; set; }
        public int StartTime { get; private set; }
        public int EndTime { get; private set; }
        public bool IsCritical { get; set; }

        public Rect Bounds { get; set; }

        public IList<int> PrereqNumbers { get; private set; }

        public IList<Task> PrereqTasks { get; private set; }

        public int PrereqCount { get; set; }
        public IList<Task> Followers { get; private set; }


        public Task(int index, int duration, string name, IEnumerable<int> prereqNumbers)
        {
            Index = index;
            Name = name;
            Duration = duration;

            PrereqNumbers = new List<int>(prereqNumbers);
            Followers = new List<Task>();
        }

        public void NumbersToTasks(IList<Task> taskList)
        {
            PrereqTasks =
              PrereqNumbers
                .Select(index => taskList[index])
                .ToList();
        }

        public void AddFollower(Task follower) => Followers.Add(follower);

        public override string ToString() => Name;

        public void SetTimes()
        {
            StartTime = PrereqTasks.Any() ? PrereqTasks.Max(t => t.EndTime) : 0;
            EndTime = StartTime + Duration;
        }

        public void MarkAsCritical()
        {
            IsCritical = true;
            PrereqTasks
                .Where(IsCriticalDependentOn)
                .ForEach(pre => pre.MarkAsCritical());
        }

        public bool IsCriticalDependentOn(Task preTask) => StartTime == preTask.EndTime;
    }
}
