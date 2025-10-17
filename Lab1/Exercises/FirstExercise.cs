using Lab1.Model;

namespace Lab1.Exercises;

public class FirstExercise
{
    public static void run()
    {
        var courses = new List<Course>
        {
            new Course(".NET", 5),
            new Course("Python", 6)
        };
        var updatedCourses = new List<Course>(courses)
        {
            new Course("Java", 7) 
        };
        var student = new Student(1, "John Doe", courses);
        var updatedStudent = student with { Courses = updatedCourses };

        Console.WriteLine("Original Student:");
        Console.WriteLine(student);

        Console.WriteLine("\nUpdated Student:");
        Console.WriteLine(updatedStudent);
    }
}