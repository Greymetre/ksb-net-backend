using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("holidays");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.Name).HasColumnName("name").HasColumnType("longtext");
        builder.Property(x => x.HolidayDate).HasColumnName("holiday_date").HasColumnType("longtext");
        builder.Property(x => x.Branch).HasColumnName("branch");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public sealed class LeaveConfiguration : IEntityTypeConfiguration<Leave>
{
    public void Configure(EntityTypeBuilder<Leave> builder)
    {
        builder.ToTable("leaves");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.FromDate).HasColumnName("from_date").HasColumnType("date");
        builder.Property(x => x.ToDate).HasColumnName("to_date").HasColumnType("date");
        builder.Property(x => x.Type).HasColumnName("type");
        builder.Property(x => x.BalType).HasColumnName("bal_type");
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.RemarkStatus).HasColumnName("remark_status");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class CompOffLeaveConfiguration : IEntityTypeConfiguration<CompOffLeave>
{
    public void Configure(EntityTypeBuilder<CompOffLeave> builder)
    {
        builder.ToTable("comp_off_leaves");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.LeaveId).HasColumnName("leave_id");
        builder.Property(x => x.CompOffDate).HasColumnName("comp_off_date").HasColumnType("date");
        builder.Property(x => x.ExpiryDate).HasColumnName("expiry_date").HasColumnType("date");
        builder.Property(x => x.IsUsed).HasColumnName("is_used");
        builder.Property(x => x.Balance).HasColumnName("balance").HasPrecision(10, 2).HasDefaultValue(1m);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class TourProgrammeConfiguration : IEntityTypeConfiguration<TourProgramme>
{
    public void Configure(EntityTypeBuilder<TourProgramme> builder)
    {
        builder.ToTable("tour_programmes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Date).HasColumnName("date").HasColumnType("date");
        builder.Property(x => x.UserId).HasColumnName("userid");
        builder.Property(x => x.Town).HasColumnName("town");
        builder.Property(x => x.District).HasColumnName("district");
        builder.Property(x => x.Objectives).HasColumnName("objectives");
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue(0);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class TourDetailConfiguration : IEntityTypeConfiguration<TourDetail>
{
    public void Configure(EntityTypeBuilder<TourDetail> builder)
    {
        builder.ToTable("tour_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TourId).HasColumnName("tourid");
        builder.Property(x => x.CityId).HasColumnName("city_id");
        builder.Property(x => x.VisitedDate).HasColumnName("visited_date").HasColumnType("date");
        builder.Property(x => x.VisitedCityId).HasColumnName("visited_cityid");
        builder.Property(x => x.LastVisited).HasColumnName("last_visited").HasColumnType("date");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class TourLogConfiguration : IEntityTypeConfiguration<TourLog>
{
    public void Configure(EntityTypeBuilder<TourLog> builder)
    {
        builder.ToTable("tour_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TourProgrammeId).HasColumnName("tour_programme_id");
        builder.Property(x => x.Action).HasColumnName("action");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.PerformedBy).HasColumnName("performed_by");
        builder.Property(x => x.Remark).HasColumnName("remark");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("attendances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.PunchinDate).HasColumnName("punchin_date").HasColumnType("date");
        builder.Property(x => x.PunchinTime).HasColumnName("punchin_time").HasColumnType("time");
        builder.Property(x => x.PunchinLongitude).HasColumnName("punchin_longitude");
        builder.Property(x => x.PunchinLatitude).HasColumnName("punchin_latitude");
        builder.Property(x => x.PunchinAddress).HasColumnName("punchin_address");
        builder.Property(x => x.PunchinImage).HasColumnName("punchin_image");
        builder.Property(x => x.PunchoutDate).HasColumnName("punchout_date").HasColumnType("date");
        builder.Property(x => x.PunchoutTime).HasColumnName("punchout_time").HasColumnType("time");
        builder.Property(x => x.PunchoutLatitude).HasColumnName("punchout_latitude");
        builder.Property(x => x.PunchoutLongitude).HasColumnName("punchout_longitude");
        builder.Property(x => x.PunchoutAddress).HasColumnName("punchout_address");
        builder.Property(x => x.PunchoutImage).HasColumnName("punchout_image");
        builder.Property(x => x.PunchinSummary).HasColumnName("punchin_summary");
        builder.Property(x => x.PunchoutSummary).HasColumnName("punchout_summary");
        builder.Property(x => x.WorkedTime).HasColumnName("worked_time");
        builder.Property(x => x.WorkingType).HasColumnName("working_type");
        builder.Property(x => x.AttendanceStatus).HasColumnName("attendance_status");
        builder.Property(x => x.RemarkStatus).HasColumnName("remark_status");
        builder.Property(x => x.ApproveRejectBy).HasColumnName("approve_reject_by");
        builder.Property(x => x.PunchinFrom).HasColumnName("punchin_from");
        builder.Property(x => x.Flag).HasColumnName("flag");
        builder.Property(x => x.TourId).HasColumnName("tourid");
        builder.Property(x => x.City).HasColumnName("city");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public sealed class BeatConfiguration : IEntityTypeConfiguration<Beat>
{
    public void Configure(EntityTypeBuilder<Beat> builder)
    {
        builder.ToTable("beats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.BeatName).HasColumnName("beat_name").HasMaxLength(250);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(450);
        builder.Property(x => x.CityId).HasColumnName("city_id").HasMaxLength(225);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}

public sealed class BeatScheduleConfiguration : IEntityTypeConfiguration<BeatSchedule>
{
    public void Configure(EntityTypeBuilder<BeatSchedule> builder)
    {
        builder.ToTable("beat_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Active).HasColumnName("active").HasMaxLength(1).HasDefaultValue("Y");
        builder.Property(x => x.BeatId).HasColumnName("beat_id");
        builder.Property(x => x.BeatDate).HasColumnName("beat_date").HasColumnType("date");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.TourId).HasColumnName("tourid");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.DeletedAt);
    }
}
