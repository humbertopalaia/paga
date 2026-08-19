namespace Paga.Domain.Entities;

/// <summary>
/// Category used to classify an expense. Scoped per user.
/// </summary>
public class ExpenseType
{
    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; }

    public ExpenseType(Guid userId, string name)
    {
        UserId = userId;
        Name = name;
    }

    // EF Core requires a parameterless constructor; kept private to enforce invariants.
    private ExpenseType()
    {
        Name = string.Empty;
    }
}
