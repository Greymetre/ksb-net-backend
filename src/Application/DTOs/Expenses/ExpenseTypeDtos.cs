using Application.DTOs.Users;

namespace Application.DTOs.Expenses;

public sealed class ExpenseTypeDto
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public int IsActive { get; set; } = 1;
    public string Active { get; set; } = "Y";
    public ulong AllowanceTypeId { get; set; }
    public string AllowanceTypeName { get; set; } = string.Empty;
    public ulong? PayrollId { get; set; }
    public string PayrollName { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

public sealed class ExpenseTypeRequestDto
{
    public string? Name { get; set; }
    public decimal? Rate { get; set; }
    public ulong? AllowanceTypeId { get; set; }
    public ulong? PayrollId { get; set; }
    public string? Active { get; set; }
}

public sealed class ExpenseTypeOptionsDto
{
    public IReadOnlyCollection<OptionDto> AllowanceTypes { get; set; } = [];
    public IReadOnlyCollection<OptionDto> Payrolls { get; set; } = [];
}

public static class ExpenseTypeLookups
{
    public static readonly IReadOnlyDictionary<ulong, string> AllowanceTypes = new Dictionary<ulong, string>
    {
        [1] = "Travelling",
        [2] = "Daily"
    };

    public static readonly IReadOnlyDictionary<ulong, string> Payrolls = new Dictionary<ulong, string>
    {
        [1] = "Grade 1",
        [2] = "Grade 2",
        [3] = "Grade 3",
        [4] = "Grade 4",
        [5] = "Grade 5"
    };

    public static string AllowanceTypeName(ulong id) =>
        AllowanceTypes.TryGetValue(id, out var name) ? name : string.Empty;

    public static string PayrollName(ulong? id) =>
        id.HasValue && Payrolls.TryGetValue(id.Value, out var name) ? name : string.Empty;
}
