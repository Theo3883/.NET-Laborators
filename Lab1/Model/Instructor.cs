namespace Lab1.Model;

class Instructor(string name, string department, string email)
{
    public string Name { get; init; } = name;
    public string Department { get; init; } = department;
    public string Email { get; init; } = email;
}