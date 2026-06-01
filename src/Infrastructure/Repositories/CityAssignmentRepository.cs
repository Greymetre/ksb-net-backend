using Application.DTOs.CityAssignments;
using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CityAssignmentRepository : ICityAssignmentRepository
{
    private const string DistributorRoleName = "Distributor";
    private const int MaxRows = 50000;
    private const int MaxOptionRows = 50000;
    private readonly AppDbContext _db;

    public CityAssignmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CityAssignmentDto>> GetAssignmentsAsync(CityAssignmentFilterDto filter, CancellationToken cancellationToken)
    {
        var query =
            from assignment in _db.UserCityAssigns.AsNoTracking()
            join user in _db.Users.AsNoTracking() on assignment.UserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            join userDesignation in _db.Designations.AsNoTracking() on user.DesignationId equals userDesignation.Id into userDesignations
            from userDesignation in userDesignations.DefaultIfEmpty()
            let effectiveReportingId = user.ReportingId ?? assignment.ReportingId
            join reporting in _db.Users.AsNoTracking() on effectiveReportingId equals reporting.Id into reportings
            from reporting in reportings.DefaultIfEmpty()
            join reportingDesignation in _db.Designations.AsNoTracking() on reporting.DesignationId equals reportingDesignation.Id into reportingDesignations
            from reportingDesignation in reportingDesignations.DefaultIfEmpty()
            join city in _db.Cities.AsNoTracking() on assignment.CityId equals city.Id into cities
            from city in cities.DefaultIfEmpty()
            join district in _db.Districts.AsNoTracking() on city.DistrictId equals district.Id into districts
            from district in districts.DefaultIfEmpty()
            join state in _db.States.AsNoTracking() on district.StateId equals state.Id into states
            from state in states.DefaultIfEmpty()
            select new { assignment, user, userDesignation, reporting, reportingDesignation, city, district, state };

        if (filter.UserId.HasValue) query = query.Where(x => x.assignment.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x =>
                (x.user.Name ?? "").Contains(search)
                || (x.reporting.Name ?? "").Contains(search)
                || (x.city.CityName ?? "").Contains(search)
                || (x.district.DistrictName ?? "").Contains(search)
                || (x.state.StateName ?? "").Contains(search));
        }

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageLength = Math.Clamp(filter.PageLength, 1, MaxRows);

        return await query
            .OrderBy(x => x.assignment.UserId)
            .ThenBy(x => x.city.CityName)
            .Skip((pageNumber - 1) * pageLength)
            .Take(pageLength)
            .Select(x => new CityAssignmentDto
            {
                Id = x.assignment.Id,
                UserId = x.assignment.UserId,
                UserName = x.user == null ? null : x.user.Name,
                UserDesignation = x.userDesignation == null ? null : x.userDesignation.DesignationName,
                ReportingId = x.user == null ? x.assignment.ReportingId : x.user.ReportingId ?? x.assignment.ReportingId,
                ReportingName = x.reporting == null ? null : x.reporting.Name,
                ReportingDesignation = x.reportingDesignation == null ? null : x.reportingDesignation.DesignationName,
                CityId = x.assignment.CityId,
                CityName = x.city.CityName,
                Grade = x.city.Grade,
                DistrictId = x.city.DistrictId,
                DistrictName = x.district.DistrictName,
                StateId = x.district.StateId,
                StateName = x.state.StateName,
                CreatedAt = x.assignment.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CityAssignmentOptionsDto> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var users = await InternalUsersQuery(_db.Users.AsNoTracking())
            .Where(x => x.Active == "Y" && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Take(MaxOptionRows)
            .Select(x => new CityAssignmentOptionDto { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        var cities = await _db.Cities.AsNoTracking()
            .Where(x => x.Active == "Y")
            .OrderBy(x => x.CityName)
            .Take(MaxOptionRows)
            .Select(x => new CityAssignmentOptionDto { Id = x.Id, Name = x.CityName })
            .ToListAsync(cancellationToken);

        return new CityAssignmentOptionsDto { Users = users, Reportings = users, Cities = cities };
    }

    public Task<User?> GetUserByIdAsync(ulong userId, CancellationToken cancellationToken) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<UserCityAssign?> GetAssignmentEntityAsync(ulong id, CancellationToken cancellationToken) =>
        _db.UserCityAssigns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<UserCityAssign?> GetAssignmentByUserCityAsync(ulong userId, ulong cityId, CancellationToken cancellationToken) =>
        _db.UserCityAssigns.FirstOrDefaultAsync(x => x.UserId == userId && x.CityId == cityId, cancellationToken);

    public async Task AddAssignmentAsync(UserCityAssign assignment, CancellationToken cancellationToken)
    {
        _db.UserCityAssigns.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAssignmentAsync(UserCityAssign assignment, CancellationToken cancellationToken)
    {
        _db.UserCityAssigns.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> UserExistsAsync(ulong userId, CancellationToken cancellationToken) =>
        InternalUsersQuery(_db.Users.AsNoTracking()).AnyAsync(x => x.Id == userId && x.Active == "Y" && !x.IsDeleted, cancellationToken);

    public Task<bool> CityExistsAsync(ulong cityId, CancellationToken cancellationToken) =>
        _db.Cities.AsNoTracking().AnyAsync(x => x.Id == cityId && x.Active == "Y", cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    private IQueryable<User> InternalUsersQuery(IQueryable<User> query) =>
        query.Where(user =>
            !user.CustomerId.HasValue
            && !_db.ModelHasRoles
                .Join(_db.Roles, modelRole => modelRole.RoleId, role => role.Id, (modelRole, role) => new { modelRole, role })
                .Any(x => x.modelRole.ModelId == user.Id && x.modelRole.ModelType == LaravelModelTypes.User && x.role.Name == DistributorRoleName));
}
