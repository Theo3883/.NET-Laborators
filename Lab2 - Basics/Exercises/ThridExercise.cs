namespace Lab2.Exercises;

using Task = Lab2.Model.Task;

public static class ThridExercise
{
    public static void Run()
    {
        var tasks = new List<Task>()
        {
            new Task("frontend", false, DateTime.Now.AddDays(3)),
            new Task("backend", true, DateTime.Now.AddDays(2)),
            new Task("database", false, DateTime.Now.AddDays(3)),
            new Task("testing", false, DateTime.Now.AddDays(5)),
            new Task("deployment", false, DateTime.Now.AddDays(7)),
        };

        Console.WriteLine("Type the name of the task: ");
        var taskName = Console.ReadLine();
        
        var existingTask = tasks.Find(task => task.Title == taskName);
        
        if (existingTask != null)
        {
            var updatedTask = existingTask with { IsCompleted = true };
            
        
            var index = tasks.IndexOf(existingTask);
            
            tasks.RemoveAt(index);
            tasks.Insert(index, updatedTask);
            
            Console.WriteLine($"Task '{taskName}' has been marked as completed.");
            
            Console.WriteLine("\nUpdated tasks:");
            foreach (var task in tasks)
            {
                Console.WriteLine($"- {task.Title}: {(task.IsCompleted ? "Completed" : "Not Completed")} (Due: {task.DueDate:yyyy-MM-dd})");
            }
        }
        else
        {
            Console.WriteLine($"Task '{taskName}' not found.");
        }
    }
    
}