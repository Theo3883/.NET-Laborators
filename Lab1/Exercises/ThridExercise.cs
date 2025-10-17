using Lab1.Model;

namespace Lab1.Exercises;

public class ThridExercise
{
    public static void run()
    {
        var students = new List<Student>();
        
        Console.WriteLine("Type the name of the student: ");
        var studentName = Console.ReadLine();
        
        students.Add(new Student(1, studentName ?? "Unknown", new List<Course>()));
        
        Console.WriteLine("Students list:");
        foreach (var studentList in students)
        {
            Console.WriteLine(studentList);
        }
    }
}