using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduling
{
  internal class Task
  {
    public int Index { get; private set; }
    public string Name { get; private set; }
    
    public IList<int> PrereqNumbers { get; private set; }

    public IList<Task> PrereqTasks { get; private set; }

    public Task(int index, string name, IEnumerable<int> prereqNumbers)
    {
      Index = index;
      Name = name;
      PrereqNumbers = new List<int>(prereqNumbers);
    }

    public void NumbersToTasks(IList<Task> taskList)
    {
      PrereqTasks =
        PrereqNumbers
          .Select(index => taskList[index])
          .ToList();
    }

    public override string ToString() => Name;
  }
}
