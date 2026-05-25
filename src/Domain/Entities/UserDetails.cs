namespace Domain.Entities;

public sealed class UserDetails : BaseEntity
{
    public string Active { get; set; } = "Y";
    public ulong? UserId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public string? MaritalStatus { get; set; }
    public string? PanNumber { get; set; }
    public string? AadharNumber { get; set; }
    public string? EmergencyNumber { get; set; }
    public string? CurrentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? FatherName { get; set; }
    public DateTime? FatherDateOfBirth { get; set; }
    public string? MotherName { get; set; }
    public DateTime? MotherDateOfBirth { get; set; }
    public DateTime? MarriageAnniversary { get; set; }
    public string? SpouseName { get; set; }
    public DateTime? SpouseDateOfBirth { get; set; }
    public string? ChildrenOne { get; set; }
    public DateTime? ChildrenOneDateOfBirth { get; set; }
    public string? ChildrenTwo { get; set; }
    public DateTime? ChildrenTwoDateOfBirth { get; set; }
    public string? ChildrenThree { get; set; }
    public DateTime? ChildrenThreeDateOfBirth { get; set; }
    public string? ChildrenFour { get; set; }
    public DateTime? ChildrenFourDateOfBirth { get; set; }
    public string? ChildrenFive { get; set; }
    public DateTime? ChildrenFiveDateOfBirth { get; set; }
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? IfscCode { get; set; }
    public decimal Salary { get; set; }
    public decimal CtcAnnual { get; set; }
    public decimal GrossSalaryMonthly { get; set; }
    public decimal LastYearIncrements { get; set; }
    public string? LastYearIncrementPercent { get; set; }
    public decimal LastYearIncrementValue { get; set; }
    public string? LastPromotion { get; set; }
    public string? PfNumber { get; set; }
    public string? UnNumber { get; set; }
    public string? EsiNumber { get; set; }
    public string? ProbationPeriod { get; set; }
    public DateTime? DateOfConfirmation { get; set; }
    public string? NoticePeriod { get; set; }
    public DateTime? DateOfLeaving { get; set; }
    public string? BiometricCode { get; set; }
    public string? OrderMails { get; set; }
    public string? OrderMailsType { get; set; }
    public string? OtherEducation { get; set; }
    public string? PreviousExp { get; set; }
    public string? CurrentCompanyTenture { get; set; }
    public string? TotalExp { get; set; }
}
