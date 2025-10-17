namespace Lab2.Model;

public class Manager(string name, string team, string email)
{
    public string Name { get; init; } = name;
    public string Team { get; init; } = team;
    public string Email { get; init; } = email;
}