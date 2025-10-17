
using Lab1.Exercises;
using Lab1.Model;
using Microsoft.VisualBasic;


FirstExercise.run();

SecondExercise.run();

ThridExercise.run();

Console. WriteLine();
FourExercise.run(new Student(1, "John", new List<Course>()));
FourExercise.run(new Course("Math", 5));


var courses = new List<Course>
{
    new Course("Math", 5),
    new Course("History", 3),
    new Course("Science", 4),
    new Course("Art", 2)
};
FiveExercise.run(courses);


