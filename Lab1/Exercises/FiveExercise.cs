using Lab1.Model;

namespace Lab1.Exercises;

public class FiveExercise
{
    public static Func<Course, bool> hasCredits = (course) => course.Credits > 3;

    private static void filteredCourses(List<Course> courses, Func<Course, bool> predicate)
    {
        var filtered = courses.Where(predicate).ToList();
        Console.WriteLine("Filtered courses:");
        foreach (var course in filtered)
        {
            Console.WriteLine(course);
        }
    }

    public static void run(List<Course> courses)
    {
        filteredCourses(courses, hasCredits);
    }
}