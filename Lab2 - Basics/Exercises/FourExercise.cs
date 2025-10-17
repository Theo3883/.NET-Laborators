using Lab2.Model;
using Task = Lab2.Model.Task;

namespace Lab2.Exercises;

public class FourExercise
{
    private static void What_is_this(object obj)
    {
        switch (obj)
        {
            case Task task:
                Console.WriteLine("Task title: " + task.Title + " Status: " + task.IsCompleted);
                break;
            case Project project:
                Console.WriteLine("Project name: " + project.Name + " Number of tasks: " + project.Tasks.Count);
                break;
            default:
                Console.WriteLine("Unknown type");
                break;
        }
    }
    public static void Run(object obj)
    {
        What_is_this(obj);
    }
}