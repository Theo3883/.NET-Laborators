using Task = Lab2.Model.Task;

namespace Lab2.Exercises;

public static class FiveExercise
{
    private static Func<Task, bool> _isOverDue = (task) => task.IsCompleted == false && task.DueDate < DateTime.Now;

    private static void FilterTasks(List<Task> tasks, Func<Task, bool> _isOverDue)
    {
        var filteredTasks = tasks.Where(FiveExercise._isOverDue);
        Console.WriteLine("Filtered tasks:");
        foreach (var task in filteredTasks)
        {
            Console.WriteLine("Task: " + task.Title + " is over due");
        }
    }

    public static void Run(List<Task> tasks)
    {
        FilterTasks(tasks, _isOverDue);
    }
}