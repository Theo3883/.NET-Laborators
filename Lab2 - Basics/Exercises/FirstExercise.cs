using System.Reflection.Metadata;
using Lab2.Model;
using Task = Lab2.Model.Task;

namespace Lab2.Exercises;

public static class FirstExercise
{
    public static void Run()
    {
        var tasks = new List<Model.Task>()
        {
            new Task("frontend", false, DateTime.Now.AddDays(3)),
            new Task("backend", true, DateTime.Now.AddDays(2)),
        };
        
        var updatedTasks = new List<Task>(tasks)
        {
            new Task("database", false, DateTime.Now.AddDays(3)),
        };
        
        var project = new Project("New Project", tasks);
        var updatedProject = project with { Tasks = updatedTasks };

        Console.WriteLine("Original Project:");
        Console.WriteLine(project);

        Console.WriteLine("\nUpdated Project:");
        Console.WriteLine(updatedProject);
    }
}