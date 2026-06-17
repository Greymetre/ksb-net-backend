using Application.DTOs.MasterData;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MasterDataRepository : IMasterDataRepository
{
    private const int MaxRows = 50000;
    private const int MaxCityRows = 50000;
    private readonly AppDbContext _dbContext;

    public MasterDataRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CountryDto>> GetCountriesAsync(string? search, CancellationToken cancellationToken, bool includeInactive = false)
    {
        var query = _dbContext.Countries.AsNoTracking().Where(x => x.DeletedAt == null && (includeInactive || x.Active == "Y"));
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.CountryName.Contains(search.Trim()));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new CountryDto
            {
                Id = x.Id,
                CountryName = x.CountryName,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CountryDto?> GetCountryAsync(ulong id, CancellationToken cancellationToken)
    {
        var country = await _dbContext.Countries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return country is null ? null : ToCountryDto(country);
    }

    public async Task<IReadOnlyCollection<CountryExportRowDto>> ExportCountriesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Countries.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Select(x => new CountryExportRowDto { Id = x.Id, CountryName = x.CountryName })
            .ToListAsync(cancellationToken);
    }

    public async Task<CountryDto> CreateCountryAsync(CountryRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var country = new Country
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            CountryName = request.CountryName!.Trim(),
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Countries.AddAsync(country, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToCountryDto(country);
    }

    public async Task<CountryDto?> UpdateCountryAsync(ulong id, CountryRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var country = await _dbContext.Countries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (country is null) return null;

        if (!string.IsNullOrWhiteSpace(request.CountryName)) country.CountryName = request.CountryName.Trim();
        var active = NormalizeActive(request.Active);
        if (active is not null) country.Active = active;
        country.UpdatedBy = actorUserId;
        country.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToCountryDto(country);
    }

    public async Task<CountryDto?> SetCountryActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var country = await _dbContext.Countries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (country is null) return null;

        country.Active = NormalizeActive(active) ?? ToggleActive(country.Active);
        country.UpdatedBy = actorUserId;
        country.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToCountryDto(country);
    }

    public async Task<bool> DeleteCountryAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var country = await _dbContext.Countries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (country is null) return false;

        country.Active = "N";
        country.DeletedAt = DateTime.UtcNow;
        country.UpdatedBy = actorUserId;
        country.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<StateDto>> GetStatesAsync(ulong? countryId, string? search, CancellationToken cancellationToken, bool includeInactive = false)
    {
        var query = _dbContext.States.AsNoTracking().Where(x => x.DeletedAt == null && (includeInactive || x.Active == "Y"));
        if (countryId.HasValue) query = query.Where(x => x.CountryId == countryId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.StateName.Contains(search.Trim()));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new StateDto
            {
                Id = x.Id,
                StateName = x.StateName,
                CountryId = x.CountryId,
                CountryName = _dbContext.Countries.Where(country => country.Id == x.CountryId).Select(country => country.CountryName).FirstOrDefault(),
                GstCode = x.GstCode,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StateDto?> GetStateAsync(ulong id, CancellationToken cancellationToken)
    {
        var state = await _dbContext.States.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return state is null ? null : ToStateDto(state);
    }

    public async Task<IReadOnlyCollection<StateExportRowDto>> ExportStatesAsync(CancellationToken cancellationToken)
    {
        return await (
            from state in _dbContext.States.AsNoTracking()
            join country in _dbContext.Countries.AsNoTracking() on state.CountryId equals country.Id into countryJoin
            from country in countryJoin.DefaultIfEmpty()
            orderby state.Id descending
            select new StateExportRowDto
            {
                Id = state.Id,
                StateName = state.StateName,
                CountryId = state.CountryId,
                CountryName = country == null ? null : country.CountryName,
                GstCode = state.GstCode
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StateDto> CreateStateAsync(StateRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var state = new State
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            StateName = request.StateName!.Trim(),
            CountryId = request.CountryId,
            GstCode = NormalizeText(request.GstCode),
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.States.AddAsync(state, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToStateDto(state);
    }

    public async Task<StateDto?> UpdateStateAsync(ulong id, StateRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var state = await _dbContext.States.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (state is null) return null;

        if (!string.IsNullOrWhiteSpace(request.StateName)) state.StateName = request.StateName.Trim();
        if (request.CountryId.HasValue) state.CountryId = request.CountryId;
        if (request.GstCode is not null) state.GstCode = NormalizeText(request.GstCode);
        var active = NormalizeActive(request.Active);
        if (active is not null) state.Active = active;
        state.UpdatedBy = actorUserId;
        state.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToStateDto(state);
    }

    public async Task<StateDto?> SetStateActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var state = await _dbContext.States.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (state is null) return null;

        state.Active = NormalizeActive(active) ?? ToggleActive(state.Active);
        state.UpdatedBy = actorUserId;
        state.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToStateDto(state);
    }

    public async Task<bool> DeleteStateAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var state = await _dbContext.States.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (state is null) return false;

        state.Active = "N";
        state.DeletedAt = DateTime.UtcNow;
        state.UpdatedBy = actorUserId;
        state.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<DistrictDto>> GetDistrictsAsync(ulong? stateId, string? search, CancellationToken cancellationToken, bool includeInactive = false)
    {
        var query = _dbContext.Districts.AsNoTracking().Where(x => x.DeletedAt == null && (includeInactive || x.Active == "Y"));
        if (stateId.HasValue) query = query.Where(x => x.StateId == stateId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.DistrictName.Contains(search.Trim()));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new DistrictDto
            {
                Id = x.Id,
                DistrictName = x.DistrictName,
                StateId = x.StateId,
                StateName = _dbContext.States.Where(state => state.Id == x.StateId).Select(state => state.StateName).FirstOrDefault(),
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DistrictDto?> GetDistrictAsync(ulong id, CancellationToken cancellationToken)
    {
        var district = await _dbContext.Districts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return district is null ? null : ToDistrictDto(district);
    }

    public async Task<IReadOnlyCollection<DistrictExportRowDto>> ExportDistrictsAsync(CancellationToken cancellationToken)
    {
        return await (
            from district in _dbContext.Districts.AsNoTracking()
            join state in _dbContext.States.AsNoTracking() on district.StateId equals state.Id into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            orderby district.Id descending
            select new DistrictExportRowDto
            {
                Id = district.Id,
                DistrictName = district.DistrictName,
                StateId = district.StateId,
                StateName = state == null ? null : state.StateName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DistrictDto> CreateDistrictAsync(DistrictRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var district = new District
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            DistrictName = request.DistrictName!.Trim(),
            StateId = request.StateId,
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Districts.AddAsync(district, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDistrictDto(district);
    }

    public async Task<DistrictDto?> UpdateDistrictAsync(ulong id, DistrictRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var district = await _dbContext.Districts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (district is null) return null;

        if (!string.IsNullOrWhiteSpace(request.DistrictName)) district.DistrictName = request.DistrictName.Trim();
        if (request.StateId.HasValue) district.StateId = request.StateId;
        var active = NormalizeActive(request.Active);
        if (active is not null) district.Active = active;
        district.UpdatedBy = actorUserId;
        district.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDistrictDto(district);
    }

    public async Task<DistrictDto?> SetDistrictActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var district = await _dbContext.Districts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (district is null) return null;

        district.Active = NormalizeActive(active) ?? ToggleActive(district.Active);
        district.UpdatedBy = actorUserId;
        district.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDistrictDto(district);
    }

    public async Task<bool> DeleteDistrictAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var district = await _dbContext.Districts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (district is null) return false;

        district.Active = "N";
        district.DeletedAt = DateTime.UtcNow;
        district.UpdatedBy = actorUserId;
        district.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<CityDto>> GetCitiesAsync(ulong? stateId, ulong? districtId, string? search, CancellationToken cancellationToken, bool includeInactive = false)
    {
        var query = _dbContext.Cities.AsNoTracking().Where(x => x.DeletedAt == null && (includeInactive || x.Active == "Y"));
        if (stateId.HasValue) query = query.Where(x => x.StateId == stateId);
        if (districtId.HasValue) query = query.Where(x => x.DistrictId == districtId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.CityName.Contains(search.Trim()));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxCityRows)
            .Select(x => new CityDto
            {
                Id = x.Id,
                CityName = x.CityName,
                DistrictId = x.DistrictId,
                DistrictName = _dbContext.Districts.Where(district => district.Id == x.DistrictId).Select(district => district.DistrictName).FirstOrDefault(),
                StateId = x.StateId,
                StateName = _dbContext.States.Where(state => state.Id == x.StateId).Select(state => state.StateName).FirstOrDefault(),
                Grade = x.Grade,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CityDto?> GetCityAsync(ulong id, CancellationToken cancellationToken)
    {
        var city = await _dbContext.Cities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return city is null ? null : ToCityDto(city);
    }

    public async Task<IReadOnlyCollection<CityExportRowDto>> ExportCitiesAsync(CancellationToken cancellationToken)
    {
        return await (
            from city in _dbContext.Cities.AsNoTracking()
            join district in _dbContext.Districts.AsNoTracking() on city.DistrictId equals district.Id into districtJoin
            from district in districtJoin.DefaultIfEmpty()
            join state in _dbContext.States.AsNoTracking() on district.StateId equals state.Id into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            orderby city.Id descending
            select new CityExportRowDto
            {
                Id = city.Id,
                CityName = city.CityName,
                DistrictId = city.DistrictId,
                DistrictName = district == null ? null : district.DistrictName,
                Grade = city.Grade,
                StateId = district == null ? null : district.StateId,
                StateName = state == null ? null : state.StateName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CityDto> CreateCityAsync(CityRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var city = new City
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            CityName = request.CityName!.Trim(),
            DistrictId = request.DistrictId,
            StateId = request.StateId,
            Grade = NormalizeText(request.Grade),
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Cities.AddAsync(city, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToCityDto(city);
    }

    public async Task<CityDto?> UpdateCityAsync(ulong id, CityRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var city = await _dbContext.Cities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (city is null) return null;

        if (!string.IsNullOrWhiteSpace(request.CityName)) city.CityName = request.CityName.Trim();
        if (request.DistrictId.HasValue) city.DistrictId = request.DistrictId;
        if (request.StateId.HasValue) city.StateId = request.StateId;
        if (request.Grade is not null) city.Grade = NormalizeText(request.Grade);
        var active = NormalizeActive(request.Active);
        if (active is not null) city.Active = active;
        city.UpdatedBy = actorUserId;
        city.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToCityDto(city);
    }

    public async Task<CityDto?> SetCityActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var city = await _dbContext.Cities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (city is null) return null;

        city.Active = NormalizeActive(active) ?? ToggleActive(city.Active);
        city.UpdatedBy = actorUserId;
        city.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToCityDto(city);
    }

    public async Task<bool> DeleteCityAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var city = await _dbContext.Cities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (city is null) return false;

        city.Active = "N";
        city.DeletedAt = DateTime.UtcNow;
        city.UpdatedBy = actorUserId;
        city.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<PincodeDto>> GetPincodesAsync(ulong? cityId, string? pincode, CancellationToken cancellationToken, bool includeInactive = false)
    {
        var query = _dbContext.Pincodes.AsNoTracking().Where(x => x.DeletedAt == null && (includeInactive || x.Active == "Y"));
        if (cityId.HasValue) query = query.Where(x => x.CityId == cityId);
        if (!string.IsNullOrWhiteSpace(pincode))
        {
            query = query.Where(x => x.PinCode.Contains(pincode.Trim()));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new PincodeDto
            {
                Id = x.Id,
                Pincode = x.PinCode,
                CityId = x.CityId,
                CityName = _dbContext.Cities.Where(city => city.Id == x.CityId).Select(city => city.CityName).FirstOrDefault(),
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PincodeDto?> GetPincodeAsync(ulong id, CancellationToken cancellationToken)
    {
        var pincode = await _dbContext.Pincodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return pincode is null ? null : ToPincodeDto(pincode);
    }

    public async Task<IReadOnlyCollection<PincodeExportRowDto>> ExportPincodesAsync(CancellationToken cancellationToken)
    {
        return await (
            from pincode in _dbContext.Pincodes.AsNoTracking()
            join city in _dbContext.Cities.AsNoTracking() on pincode.CityId equals city.Id into cityJoin
            from city in cityJoin.DefaultIfEmpty()
            orderby pincode.Id descending
            select new PincodeExportRowDto
            {
                Id = pincode.Id,
                Pincode = pincode.PinCode,
                CityId = pincode.CityId,
                CityName = city == null ? null : city.CityName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PincodeDto> CreatePincodeAsync(PincodeRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var pincode = new Pincode
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            PinCode = request.Pincode!.Trim(),
            CityId = request.CityId,
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Pincodes.AddAsync(pincode, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToPincodeDto(pincode);
    }

    public async Task<PincodeDto?> UpdatePincodeAsync(ulong id, PincodeRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var pincode = await _dbContext.Pincodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pincode is null) return null;

        if (!string.IsNullOrWhiteSpace(request.Pincode)) pincode.PinCode = request.Pincode.Trim();
        if (request.CityId.HasValue) pincode.CityId = request.CityId;
        var active = NormalizeActive(request.Active);
        if (active is not null) pincode.Active = active;
        pincode.UpdatedBy = actorUserId;
        pincode.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToPincodeDto(pincode);
    }

    public async Task<PincodeDto?> SetPincodeActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var pincode = await _dbContext.Pincodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pincode is null) return null;

        pincode.Active = NormalizeActive(active) ?? ToggleActive(pincode.Active);
        pincode.UpdatedBy = actorUserId;
        pincode.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToPincodeDto(pincode);
    }

    public async Task<bool> DeletePincodeAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var pincode = await _dbContext.Pincodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pincode is null) return false;

        pincode.Active = "N";
        pincode.DeletedAt = DateTime.UtcNow;
        pincode.UpdatedBy = actorUserId;
        pincode.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<BranchDto>> GetBranchesAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Branches.AsNoTracking().Where(x => x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(x => x.BranchName.Contains(normalized) || (x.BranchCode != null && x.BranchCode.Contains(normalized)));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new BranchDto
            {
                Id = x.Id,
                BranchName = x.BranchName,
                BranchCode = x.BranchCode,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BranchDto>> ExportBranchesAsync(CancellationToken cancellationToken) =>
        await GetBranchesAsync(null, cancellationToken);

    public async Task<BranchDto?> GetBranchAsync(ulong id, CancellationToken cancellationToken)
    {
        return await _dbContext.Branches.AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new BranchDto
            {
                Id = x.Id,
                BranchName = x.BranchName,
                BranchCode = x.BranchCode,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> BranchNameExistsAsync(string branchName, ulong? excludeId, CancellationToken cancellationToken)
    {
        var normalized = branchName.Trim().ToLower();
        return await _dbContext.Branches.AsNoTracking()
            .AnyAsync(x => x.DeletedAt == null
                && (!excludeId.HasValue || x.Id != excludeId.Value)
                && x.BranchName.ToLower() == normalized, cancellationToken);
    }

    public async Task<BranchDto> CreateBranchAsync(BranchRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var branch = new Branch
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            BranchName = request.BranchName!.Trim(),
            BranchCode = NormalizeText(request.BranchCode),
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Branches.AddAsync(branch, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToBranchDto(branch);
    }

    public async Task<BranchDto?> UpdateBranchAsync(ulong id, BranchRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var branch = await _dbContext.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (branch is null) return null;

        if (!string.IsNullOrWhiteSpace(request.BranchName)) branch.BranchName = request.BranchName.Trim();
        if (request.BranchCode is not null) branch.BranchCode = NormalizeText(request.BranchCode);
        var active = NormalizeActive(request.Active);
        if (active is not null) branch.Active = active;
        branch.UpdatedBy = actorUserId;
        branch.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToBranchDto(branch);
    }

    public async Task<BranchDto?> SetBranchActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var branch = await _dbContext.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (branch is null) return null;

        branch.Active = NormalizeActive(active) ?? ToggleActive(branch.Active);
        branch.UpdatedBy = actorUserId;
        branch.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToBranchDto(branch);
    }

    public async Task<bool> DeleteBranchAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var branch = await _dbContext.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (branch is null) return false;

        branch.Active = "N";
        branch.DeletedAt = DateTime.UtcNow;
        branch.UpdatedBy = actorUserId;
        branch.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<DivisionDto>> GetDivisionsAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Divisions.AsNoTracking().Where(x => x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.DivisionName.Contains(search.Trim()));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new DivisionDto
            {
                Id = x.Id,
                DivisionName = x.DivisionName,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DivisionDto>> ExportDivisionsAsync(CancellationToken cancellationToken) =>
        await GetDivisionsAsync(null, cancellationToken);

    public async Task<DivisionDto?> GetDivisionAsync(ulong id, CancellationToken cancellationToken)
    {
        return await _dbContext.Divisions.AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new DivisionDto
            {
                Id = x.Id,
                DivisionName = x.DivisionName,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DivisionNameExistsAsync(string divisionName, ulong? excludeId, CancellationToken cancellationToken)
    {
        var normalized = divisionName.Trim().ToLower();
        return await _dbContext.Divisions.AsNoTracking()
            .AnyAsync(x => x.DeletedAt == null
                && (!excludeId.HasValue || x.Id != excludeId.Value)
                && x.DivisionName.ToLower() == normalized, cancellationToken);
    }

    public async Task<DivisionDto> CreateDivisionAsync(DivisionRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var division = new Division
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            DivisionName = request.DivisionName!.Trim(),
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Divisions.AddAsync(division, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDivisionDto(division);
    }

    public async Task<DivisionDto?> UpdateDivisionAsync(ulong id, DivisionRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var division = await _dbContext.Divisions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (division is null) return null;

        if (!string.IsNullOrWhiteSpace(request.DivisionName)) division.DivisionName = request.DivisionName.Trim();
        var active = NormalizeActive(request.Active);
        if (active is not null) division.Active = active;
        division.UpdatedBy = actorUserId;
        division.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDivisionDto(division);
    }

    public async Task<DivisionDto?> SetDivisionActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var division = await _dbContext.Divisions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (division is null) return null;

        division.Active = NormalizeActive(active) ?? ToggleActive(division.Active);
        division.UpdatedBy = actorUserId;
        division.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDivisionDto(division);
    }

    public async Task<bool> DeleteDivisionAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var division = await _dbContext.Divisions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (division is null) return false;

        division.Active = "N";
        division.DeletedAt = DateTime.UtcNow;
        division.UpdatedBy = actorUserId;
        division.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<DesignationDto>> GetDesignationsAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Designations.AsNoTracking().Where(x => x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.DesignationName.Contains(search.Trim()));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new DesignationDto
            {
                Id = x.Id,
                DesignationName = x.DesignationName,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DesignationDto>> ExportDesignationsAsync(CancellationToken cancellationToken) =>
        await GetDesignationsAsync(null, cancellationToken);

    public async Task<DesignationDto?> GetDesignationAsync(ulong id, CancellationToken cancellationToken)
    {
        return await _dbContext.Designations.AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new DesignationDto
            {
                Id = x.Id,
                DesignationName = x.DesignationName,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DesignationNameExistsAsync(string designationName, ulong? excludeId, CancellationToken cancellationToken)
    {
        var normalized = designationName.Trim().ToLower();
        return await _dbContext.Designations.AsNoTracking()
            .AnyAsync(x => x.DeletedAt == null
                && (!excludeId.HasValue || x.Id != excludeId.Value)
                && x.DesignationName.ToLower() == normalized, cancellationToken);
    }

    public async Task<DesignationDto> CreateDesignationAsync(DesignationRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var designation = new Designation
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            DesignationName = request.DesignationName!.Trim(),
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Designations.AddAsync(designation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDesignationDto(designation);
    }

    public async Task<DesignationDto?> UpdateDesignationAsync(ulong id, DesignationRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var designation = await _dbContext.Designations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (designation is null) return null;

        if (!string.IsNullOrWhiteSpace(request.DesignationName)) designation.DesignationName = request.DesignationName.Trim();
        var active = NormalizeActive(request.Active);
        if (active is not null) designation.Active = active;
        designation.UpdatedBy = actorUserId;
        designation.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDesignationDto(designation);
    }

    public async Task<DesignationDto?> SetDesignationActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var designation = await _dbContext.Designations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (designation is null) return null;

        designation.Active = NormalizeActive(active) ?? ToggleActive(designation.Active);
        designation.UpdatedBy = actorUserId;
        designation.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDesignationDto(designation);
    }

    public async Task<bool> DeleteDesignationAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var designation = await _dbContext.Designations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (designation is null) return false;

        designation.Active = "N";
        designation.DeletedAt = DateTime.UtcNow;
        designation.UpdatedBy = actorUserId;
        designation.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<DepartmentDto>> GetDepartmentsAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Departments.AsNoTracking().Where(x => x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search.Trim()));
        }

        return await query
            .OrderByDescending(x => x.Id)
            .Take(MaxRows)
            .Select(x => new DepartmentDto
            {
                Id = x.Id,
                Name = x.Name,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DepartmentDto>> ExportDepartmentsAsync(CancellationToken cancellationToken) =>
        await GetDepartmentsAsync(null, cancellationToken);

    public async Task<DepartmentDto?> GetDepartmentAsync(ulong id, CancellationToken cancellationToken)
    {
        return await _dbContext.Departments.AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null)
            .Select(x => new DepartmentDto
            {
                Id = x.Id,
                Name = x.Name,
                Active = x.Active,
                CreatedBy = x.CreatedBy,
                CreatedByName = _dbContext.Users.Where(user => user.Id == x.CreatedBy).Select(user => user.Name).FirstOrDefault(),
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DepartmentNameExistsAsync(string name, ulong? excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return await _dbContext.Departments.AsNoTracking()
            .AnyAsync(x => x.DeletedAt == null
                && (!excludeId.HasValue || x.Id != excludeId.Value)
                && x.Name.ToLower() == normalized, cancellationToken);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(DepartmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var department = new Department
        {
            Active = NormalizeActive(request.Active) ?? "Y",
            Name = request.Name!.Trim(),
            CreatedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Departments.AddAsync(department, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDepartmentDto(department);
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(ulong id, DepartmentRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var department = await _dbContext.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (department is null) return null;

        if (!string.IsNullOrWhiteSpace(request.Name)) department.Name = request.Name.Trim();
        var active = NormalizeActive(request.Active);
        if (active is not null) department.Active = active;
        department.UpdatedBy = actorUserId;
        department.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDepartmentDto(department);
    }

    public async Task<DepartmentDto?> SetDepartmentActiveAsync(ulong id, string? active, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var department = await _dbContext.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (department is null) return null;

        department.Active = NormalizeActive(active) ?? ToggleActive(department.Active);
        department.UpdatedBy = actorUserId;
        department.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDepartmentDto(department);
    }

    public async Task<bool> DeleteDepartmentAsync(ulong id, ulong? actorUserId, CancellationToken cancellationToken)
    {
        var department = await _dbContext.Departments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (department is null) return false;

        department.Active = "N";
        department.DeletedAt = DateTime.UtcNow;
        department.UpdatedBy = actorUserId;
        department.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<LocationDetailsDto>> GetLocationDetailsAsync(string? pincode, ulong? stateId, ulong? cityId, string? city, CancellationToken cancellationToken)
    {
        var normalizedPincode = pincode?.Trim();
        var normalizedCity = city?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedPincode))
        {
            return await GetDetailsByPincodeAsync(normalizedPincode, cancellationToken);
        }

        if (cityId.HasValue || !string.IsNullOrWhiteSpace(normalizedCity))
        {
            return await GetDetailsByCityAsync(cityId, normalizedCity, cancellationToken);
        }

        return await GetDetailsByStateAsync(stateId!.Value, cancellationToken);
    }

    private async Task<IReadOnlyCollection<LocationDetailsDto>> GetDetailsByPincodeAsync(string pincode, CancellationToken cancellationToken)
    {
        var rows = await (
            from pin in _dbContext.Pincodes.AsNoTracking()
            join city in _dbContext.Cities.AsNoTracking() on pin.CityId equals city.Id into cityJoin
            from city in cityJoin.DefaultIfEmpty()
            join district in _dbContext.Districts.AsNoTracking() on city.DistrictId equals district.Id into districtJoin
            from district in districtJoin.DefaultIfEmpty()
            join state in _dbContext.States.AsNoTracking() on city.StateId equals state.Id into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            join country in _dbContext.Countries.AsNoTracking() on state.CountryId equals country.Id into countryJoin
            from country in countryJoin.DefaultIfEmpty()
            where pin.Active == "Y" && pin.PinCode == pincode
            orderby city.CityName, pin.PinCode
            select new { pin, city, district, state, country })
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        return rows.Select(row => ToLocationDetails(row.country, row.state, row.district, row.city, [row.pin])).ToList();
    }

    private async Task<IReadOnlyCollection<LocationDetailsDto>> GetDetailsByCityAsync(ulong? cityId, string? city, CancellationToken cancellationToken)
    {
        var rows = await (
            from cityRow in _dbContext.Cities.AsNoTracking()
            join district in _dbContext.Districts.AsNoTracking() on cityRow.DistrictId equals district.Id into districtJoin
            from district in districtJoin.DefaultIfEmpty()
            join state in _dbContext.States.AsNoTracking() on cityRow.StateId equals state.Id into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            join country in _dbContext.Countries.AsNoTracking() on state.CountryId equals country.Id into countryJoin
            from country in countryJoin.DefaultIfEmpty()
            where cityRow.Active == "Y"
                && (!cityId.HasValue || cityRow.Id == cityId)
                && (string.IsNullOrWhiteSpace(city) || cityRow.CityName.Contains(city))
            orderby cityRow.CityName
            select new { city = cityRow, district, state, country })
            .Take(50)
            .ToListAsync(cancellationToken);

        var cityIds = rows.Select(x => x.city.Id).ToArray();
        var pincodes = await _dbContext.Pincodes.AsNoTracking()
            .Where(x => x.Active == "Y" && x.CityId.HasValue && cityIds.Contains(x.CityId.Value))
            .OrderBy(x => x.PinCode)
            .Select(x => new PincodeDto { Id = x.Id, Pincode = x.PinCode, CityId = x.CityId, Active = x.Active })
            .ToListAsync(cancellationToken);

        return rows.Select(row =>
        {
            var cityPincodes = pincodes.Where(x => x.CityId == row.city.Id).ToArray();
            return ToLocationDetails(row.country, row.state, row.district, row.city, cityPincodes);
        }).ToList();
    }

    private async Task<IReadOnlyCollection<LocationDetailsDto>> GetDetailsByStateAsync(ulong stateId, CancellationToken cancellationToken)
    {
        var rows = await (
            from cityRow in _dbContext.Cities.AsNoTracking()
            join district in _dbContext.Districts.AsNoTracking() on cityRow.DistrictId equals district.Id into districtJoin
            from district in districtJoin.DefaultIfEmpty()
            join state in _dbContext.States.AsNoTracking() on cityRow.StateId equals state.Id into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            join country in _dbContext.Countries.AsNoTracking() on state.CountryId equals country.Id into countryJoin
            from country in countryJoin.DefaultIfEmpty()
            where cityRow.Active == "Y" && cityRow.StateId == stateId
            orderby cityRow.CityName
            select new { city = cityRow, district, state, country })
            .Take(MaxCityRows)
            .ToListAsync(cancellationToken);

        var cityIds = rows.Select(x => x.city.Id).ToArray();
        var pincodes = await _dbContext.Pincodes.AsNoTracking()
            .Where(x => x.Active == "Y" && x.CityId.HasValue && cityIds.Contains(x.CityId.Value))
            .OrderBy(x => x.PinCode)
            .Select(x => new PincodeDto { Id = x.Id, Pincode = x.PinCode, CityId = x.CityId, Active = x.Active })
            .ToListAsync(cancellationToken);

        return rows.Select(row =>
        {
            var cityPincodes = pincodes.Where(x => x.CityId == row.city.Id).ToArray();
            return ToLocationDetails(row.country, row.state, row.district, row.city, cityPincodes);
        }).ToList();
    }

    private static LocationDetailsDto ToLocationDetails(Domain.Entities.Country? country, Domain.Entities.State? state, Domain.Entities.District? district, Domain.Entities.City? city, IEnumerable<Domain.Entities.Pincode> pincodes)
    {
        return ToLocationDetails(
            country,
            state,
            district,
            city,
            pincodes.Select(x => ToPincodeDto(x)));
    }

    private static LocationDetailsDto ToLocationDetails(Domain.Entities.Country? country, Domain.Entities.State? state, Domain.Entities.District? district, Domain.Entities.City? city, IEnumerable<PincodeDto> pincodes)
    {
        return new LocationDetailsDto
        {
            Country = country is null ? null : ToCountryDto(country),
            State = state is null ? null : ToStateDto(state),
            District = district is null ? null : ToDistrictDto(district),
            City = city is null ? null : ToCityDto(city),
            Pincodes = pincodes.ToArray()
        };
    }

    private static CountryDto ToCountryDto(Country country) => new()
    {
        Id = country.Id,
        CountryName = country.CountryName,
        Active = country.Active,
        CreatedBy = country.CreatedBy,
        CreatedAt = country.CreatedAt
    };

    private static StateDto ToStateDto(State state) => new()
    {
        Id = state.Id,
        StateName = state.StateName,
        CountryId = state.CountryId,
        GstCode = state.GstCode,
        Active = state.Active,
        CreatedBy = state.CreatedBy,
        CreatedAt = state.CreatedAt
    };

    private static DistrictDto ToDistrictDto(District district) => new()
    {
        Id = district.Id,
        DistrictName = district.DistrictName,
        StateId = district.StateId,
        Active = district.Active,
        CreatedBy = district.CreatedBy,
        CreatedAt = district.CreatedAt
    };

    private static CityDto ToCityDto(City city) => new()
    {
        Id = city.Id,
        CityName = city.CityName,
        DistrictId = city.DistrictId,
        StateId = city.StateId,
        Grade = city.Grade,
        Active = city.Active,
        CreatedBy = city.CreatedBy,
        CreatedAt = city.CreatedAt
    };

    private static PincodeDto ToPincodeDto(Pincode pincode) => new()
    {
        Id = pincode.Id,
        Pincode = pincode.PinCode,
        CityId = pincode.CityId,
        Active = pincode.Active,
        CreatedBy = pincode.CreatedBy,
        CreatedAt = pincode.CreatedAt
    };

    private static BranchDto ToBranchDto(Branch branch) => new()
    {
        Id = branch.Id,
        BranchName = branch.BranchName,
        BranchCode = branch.BranchCode,
        Active = branch.Active,
        CreatedBy = branch.CreatedBy,
        CreatedAt = branch.CreatedAt
    };

    private static DivisionDto ToDivisionDto(Division division) => new()
    {
        Id = division.Id,
        DivisionName = division.DivisionName,
        Active = division.Active,
        CreatedBy = division.CreatedBy,
        CreatedAt = division.CreatedAt
    };

    private static DesignationDto ToDesignationDto(Designation designation) => new()
    {
        Id = designation.Id,
        DesignationName = designation.DesignationName,
        Active = designation.Active,
        CreatedBy = designation.CreatedBy,
        CreatedAt = designation.CreatedAt
    };

    private static DepartmentDto ToDepartmentDto(Department department) => new()
    {
        Id = department.Id,
        Name = department.Name,
        Active = department.Active,
        CreatedBy = department.CreatedBy,
        CreatedAt = department.CreatedAt
    };

    private static string? NormalizeText(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? NormalizeActive(string? active)
    {
        if (string.IsNullOrWhiteSpace(active)) return null;
        return active.Trim().Equals("N", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";
    }

    private static string ToggleActive(string active) =>
        active.Equals("Y", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";
}
