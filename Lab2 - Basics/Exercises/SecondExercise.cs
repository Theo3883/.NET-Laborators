using Lab2.Model;

namespace Lab2.Exercises;

public static class SecondExercise 
{
    public static void Run()
    {
        var manager = new Manager("John Doe", ".NET", "john.doe@uaic.ro");

        Console.WriteLine("Manager Details:");
        Console.WriteLine("Name: " + manager.Name);
        Console.WriteLine("Team: " + manager.Team);
        Console.WriteLine("Email: " + manager.Email);
    }
}