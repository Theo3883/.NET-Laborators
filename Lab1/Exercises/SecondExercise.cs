using Lab1.Model;

namespace Lab1.Exercises;

public class SecondExercise
{
    public static void run()
    {
        var instructor = new Instructor("John Doe", "teacher", "hatzculapte@uaic.ro");

        Console.WriteLine("\nInstructor: Name: " + instructor.Name + " Department:  " + instructor.Department +
                          " Email: " + instructor.Email);
    }
}