using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.DTOs.Auth;

public sealed class SignupRequestDto
{
    public string? Active { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? NotificationId { get; set; }
    public string? DeviceType { get; set; }
    public string? Gender { get; set; }
    public string? ProfileImage { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? BaseLocationCoordinates { get; set; }
    public string? Location { get; set; }

    [JsonPropertyName("branch_id")]
    public JsonElement BranchId { get; set; }

    public ulong? PrimaryBranchId { get; set; }

    [JsonPropertyName("branch_show")]
    public JsonElement BranchShow { get; set; }

    public ulong? DepartmentId { get; set; }
    public string? EmployeeCodes { get; set; }
    public ulong? DesignationId { get; set; }
    public ulong? DivisionId { get; set; }

    [JsonPropertyName("reportingid")]
    public ulong? ReportingId { get; set; }

    [JsonPropertyName("show_attandance_report")]
    public JsonElement ShowAttandanceReport { get; set; }

    public string? Payroll { get; set; }
    public ulong? WarehouseId { get; set; }

    [JsonPropertyName("customerid")]
    public ulong? CustomerId { get; set; }

    public decimal? LeaveBalance { get; set; }
    public decimal? EarnedLeaveBalance { get; set; }
    public decimal? CasualLeaveBalance { get; set; }
    public decimal? SickLeaveBalance { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public string? Grade { get; set; }
    public string? BloodGroup { get; set; }
    public string? PersonalNumber { get; set; }
    public string? SalesType { get; set; }

    public string? MaritalStatus { get; set; }
    public DateTime? DateOfBirth { get; set; }
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
    public decimal? Salary { get; set; }
    public decimal? CtcAnnual { get; set; }
    public decimal? GrossSalaryMonthly { get; set; }
    public decimal? LastYearIncrements { get; set; }
    public string? LastYearIncrementPercent { get; set; }
    public decimal? LastYearIncrementValue { get; set; }
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

    [JsonPropertyName("order_mails_type")]
    public JsonElement OrderMailsType { get; set; }

    public string? OtherEducation { get; set; }
    public string? PreviousExp { get; set; }

    [JsonPropertyName("current_company_tenture")]
    public string? CurrentCompanyTenture { get; set; }

    public string? TotalExp { get; set; }

    public JsonElement Roles { get; set; }
    public JsonElement Cities { get; set; }
    public JsonElement EducationDetail { get; set; }
}
