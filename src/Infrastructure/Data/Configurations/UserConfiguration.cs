using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(250);
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(250);
        builder.Property(x => x.Mobile).HasColumnName("mobile").HasMaxLength(11);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at");
        builder.Property(x => x.Password).HasColumnName("password").HasMaxLength(255);
        builder.Property(x => x.PasswordString).HasColumnName("password_string").HasMaxLength(255);
        builder.Property(x => x.RememberToken).HasColumnName("remember_token").HasMaxLength(100);
        builder.Property(x => x.NotificationId).HasColumnName("notification_id").HasMaxLength(450).HasDefaultValue("");
        builder.Property(x => x.DeviceType).HasColumnName("device_type").HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(20).HasDefaultValue("");
        builder.Property(x => x.ProfileImage).HasColumnName("profile_image").HasMaxLength(350).HasDefaultValue("");
        builder.Property(x => x.Latitude).HasColumnName("latitude").HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.Longitude).HasColumnName("longitude").HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.UserCode).HasColumnName("user_code").HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.Location).HasColumnName("location").HasMaxLength(250).HasDefaultValue("");
        builder.Property(x => x.ReportingId).HasColumnName("reportingid");
        builder.Property(x => x.RegionId).HasColumnName("region_id");
        builder.Property(x => x.EmployeeCodes).HasColumnName("employee_codes").HasMaxLength(250);
        builder.Property(x => x.BranchId).HasColumnName("branch_id").HasMaxLength(255);
        builder.Property(x => x.PrimaryBranchId).HasColumnName("primary_branch_id");
        builder.Property(x => x.BranchShow).HasColumnName("branch_show").HasMaxLength(50);
        builder.Property(x => x.DesignationId).HasColumnName("designation_id");
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.DivisionId).HasColumnName("division_id");
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.SalesType).HasColumnName("sales_type").HasMaxLength(20).HasDefaultValue("");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.Payroll).HasColumnName("payroll").HasMaxLength(255);
        builder.Property(x => x.LeaveBalance).HasColumnName("leave_balance").HasPrecision(18, 2);
        builder.Property(x => x.CompbOff).HasColumnName("compb_off").HasPrecision(18, 2);
        builder.Property(x => x.Grade).HasColumnName("grade").HasMaxLength(255);
        builder.Property(x => x.BloodGroup).HasColumnName("blood_group").HasMaxLength(255);
        builder.Property(x => x.PersonalNumber).HasColumnName("personal_number").HasMaxLength(255);
        builder.Property(x => x.CustomerId).HasColumnName("customerid");
        builder.Property(x => x.ShowAttandanceReport).HasColumnName("show_attandance_report").HasMaxLength(10);
        builder.Property(x => x.EarnedLeaveBalance).HasColumnName("earned_leave_balance").HasPrecision(18, 2);
        builder.Property(x => x.CasualLeaveBalance).HasColumnName("casual_leave_balance").HasPrecision(18, 2);
        builder.Property(x => x.SickLeaveBalance).HasColumnName("sick_leave_balance").HasPrecision(18, 2);
        builder.Property(x => x.DateOfJoining).HasColumnName("date_of_joining");
        builder.Property(x => x.LastLeaveAccrualDate).HasColumnName("last_leave_accrual_date");
        builder.Property(x => x.EarnedLeaveClaimActivatedAt).HasColumnName("earned_leave_claim_activated_at");
        builder.Property(x => x.ClaimableEarnedLeaveBalance).HasColumnName("claimable_earned_leave_balance").HasPrecision(18, 2);
        builder.Property(x => x.IsDeleted).HasColumnName("isDeleted");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Mobile).IsUnique();
        builder.HasIndex(x => x.ReportingId);
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public sealed class UserDetailsConfiguration : IEntityTypeConfiguration<UserDetails>
{
    public void Configure(EntityTypeBuilder<UserDetails> builder)
    {
        builder.ToTable("user_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(x => x.DateOfJoining).HasColumnName("date_of_joining");
        builder.Property(x => x.MaritalStatus).HasColumnName("marital_status").HasMaxLength(50).HasDefaultValue("");
        builder.Property(x => x.PanNumber).HasColumnName("pan_number").HasMaxLength(255);
        builder.Property(x => x.AadharNumber).HasColumnName("aadhar_number").HasMaxLength(255);
        builder.Property(x => x.EmergencyNumber).HasColumnName("emergency_number").HasMaxLength(255);
        builder.Property(x => x.CurrentAddress).HasColumnName("current_address").HasColumnType("text");
        builder.Property(x => x.PermanentAddress).HasColumnName("permanent_address").HasColumnType("text");
        builder.Property(x => x.FatherName).HasColumnName("father_name").HasMaxLength(255);
        builder.Property(x => x.FatherDateOfBirth).HasColumnName("father_date_of_birth");
        builder.Property(x => x.MotherName).HasColumnName("mother_name").HasMaxLength(255);
        builder.Property(x => x.MotherDateOfBirth).HasColumnName("mother_date_of_birth");
        builder.Property(x => x.MarriageAnniversary).HasColumnName("marriage_anniversary");
        builder.Property(x => x.SpouseName).HasColumnName("spouse_name").HasMaxLength(255);
        builder.Property(x => x.SpouseDateOfBirth).HasColumnName("spouse_date_of_birth");
        builder.Property(x => x.ChildrenOne).HasColumnName("children_one").HasMaxLength(255);
        builder.Property(x => x.ChildrenOneDateOfBirth).HasColumnName("children_one_date_of_birth");
        builder.Property(x => x.ChildrenTwo).HasColumnName("children_two").HasMaxLength(255);
        builder.Property(x => x.ChildrenTwoDateOfBirth).HasColumnName("children_two_date_of_birth");
        builder.Property(x => x.ChildrenThree).HasColumnName("children_three").HasMaxLength(255);
        builder.Property(x => x.ChildrenThreeDateOfBirth).HasColumnName("children_three_date_of_birth");
        builder.Property(x => x.ChildrenFour).HasColumnName("children_four").HasMaxLength(255);
        builder.Property(x => x.ChildrenFourDateOfBirth).HasColumnName("children_four_date_of_birth");
        builder.Property(x => x.ChildrenFive).HasColumnName("children_five").HasMaxLength(255);
        builder.Property(x => x.ChildrenFiveDateOfBirth).HasColumnName("children_five_date_of_birth");
        builder.Property(x => x.AccountNumber).HasColumnName("account_number").HasMaxLength(255);
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(255);
        builder.Property(x => x.IfscCode).HasColumnName("ifsc_code").HasMaxLength(255);
        builder.Property(x => x.Salary).HasColumnName("salary").HasPrecision(18, 2);
        builder.Property(x => x.CtcAnnual).HasColumnName("ctc_annual").HasPrecision(18, 2);
        builder.Property(x => x.GrossSalaryMonthly).HasColumnName("gross_salary_monthly").HasPrecision(18, 2);
        builder.Property(x => x.LastYearIncrements).HasColumnName("last_year_increments").HasPrecision(18, 2);
        builder.Property(x => x.LastYearIncrementPercent).HasColumnName("last_year_increment_percent").HasMaxLength(255);
        builder.Property(x => x.LastYearIncrementValue).HasColumnName("last_year_increment_value").HasPrecision(18, 2);
        builder.Property(x => x.LastPromotion).HasColumnName("last_promotion").HasMaxLength(255);
        builder.Property(x => x.PfNumber).HasColumnName("pf_number").HasMaxLength(255);
        builder.Property(x => x.UnNumber).HasColumnName("un_number").HasMaxLength(255);
        builder.Property(x => x.EsiNumber).HasColumnName("esi_number").HasMaxLength(255);
        builder.Property(x => x.ProbationPeriod).HasColumnName("probation_period").HasMaxLength(255);
        builder.Property(x => x.DateOfConfirmation).HasColumnName("date_of_confirmation");
        builder.Property(x => x.NoticePeriod).HasColumnName("notice_period").HasMaxLength(255);
        builder.Property(x => x.DateOfLeaving).HasColumnName("date_of_leaving");
        builder.Property(x => x.BiometricCode).HasColumnName("biometric_code").HasMaxLength(255);
        builder.Property(x => x.OrderMails).HasColumnName("order_mails").HasColumnType("text");
        builder.Property(x => x.OrderMailsType).HasColumnName("order_mails_type").HasMaxLength(255);
        builder.Property(x => x.OtherEducation).HasColumnName("other_education").HasMaxLength(255);
        builder.Property(x => x.PreviousExp).HasColumnName("previous_exp").HasMaxLength(255);
        builder.Property(x => x.CurrentCompanyTenture).HasColumnName("current_company_tenture").HasMaxLength(255);
        builder.Property(x => x.TotalExp).HasColumnName("total_exp").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(x => x.UserId);
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public sealed class UserCityAssignConfiguration : IEntityTypeConfiguration<UserCityAssign>
{
    public void Configure(EntityTypeBuilder<UserCityAssign> builder)
    {
        builder.ToTable("user_city_assigns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("userid");
        builder.Property(x => x.ReportingId).HasColumnName("reportingid");
        builder.Property(x => x.CityId).HasColumnName("city_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ReportingId);
        builder.HasIndex(x => x.CityId);
    }
}

public sealed class UserEducationConfiguration : IEntityTypeConfiguration<UserEducation>
{
    public void Configure(EntityTypeBuilder<UserEducation> builder)
    {
        builder.ToTable("user_education");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.EducationTypeId).HasColumnName("education_type_id");
        builder.Property(x => x.DegreeName).HasColumnName("degree_name").HasMaxLength(255);
        builder.Property(x => x.BoardName).HasColumnName("board_name").HasMaxLength(255);
        builder.Property(x => x.Percentage).HasColumnName("percentage").HasMaxLength(255);
        builder.Property(x => x.Grade).HasColumnName("grade").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(x => x.UserId);
    }
}
