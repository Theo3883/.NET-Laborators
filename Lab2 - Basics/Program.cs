using Lab2.Exercises;
using Lab2.Model;
using Task = Lab2.Model.Task;

FirstExercise.Run();
Console.WriteLine();

SecondExercise.Run();
Console.WriteLine();

ThridExercise.Run();


Console. WriteLine();
FourExercise.Run(new Task("Sample Task", false, DateTime.Now.AddDays(4)));
FourExercise.Run(new Project("Sample Project", new List<Task>
{
    new Task("Task 1", false, DateTime.Now.AddDays(2)),
    new Task("Task 2", true, DateTime.Now.AddDays(3))
}));

var tasks = new List<Task>
{
    new Task("frontend", false, DateTime.Now.AddDays(-1)),
    new Task("backend", true, DateTime.Now.AddDays(2)),
    new Task("database", false, DateTime.Now.AddDays(-3)),
    new Task("testing", false, DateTime.Now.AddDays(5)),
    new Task("deployment", false, DateTime.Now.AddDays(-7)),
};
FiveExercise.Run(tasks);