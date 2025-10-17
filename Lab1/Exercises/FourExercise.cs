using Lab1.Model;

namespace Lab1.Exercises;

public class FourExercise
{
    private static void what_is_this(object obj)
    {
        switch (obj)
        {
            case Student student:
                Console.WriteLine("Student name: " + student.Name + "Number of courses: " + student.Courses.Count);
                break;
            case Course course:
                Console.WriteLine("Course title: " + course.Title + " Credits: " + course.Credits);
                break;
            default:
                Console.WriteLine("Unknown type");
                break;
        }
    }

    public static void run(object obj)
    {
        what_is_this(obj);
    }
}