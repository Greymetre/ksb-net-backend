using System.Text.Json;
using Application.Common;
using Application.DTOs.LoyaltySchemes;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Services;

public sealed class LoyaltySchemeService : ILoyaltySchemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] SchemeTags = ["Regular", "Booster"];
    private static readonly string[] CustomerTypes = ["Retailer", "Plumber", "Retailer + Plumber", "Sub-Dealer", "Distributor"];
    private static readonly string[] AreaScopes = ["All", "Branch", "Zone", "State", "Customer"];
    private static readonly string[] BasedOnOptions = ["Value", "Percentage"];
    private static readonly string[] StatusOptions = ["Draft", "Live", "Ended"];
    private readonly ILoyaltySchemeRepository _repository;

    public LoyaltySchemeService(ILoyaltySchemeRepository repository)
    {
        _repository = repository;
    }

    public async Task<LaravelApiResponse> GetSchemesAsync(LoyaltySchemeFilterDto filter, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("schemes", await _repository.GetSchemesAsync(filter, cancellationToken));

    public async Task<LaravelApiResponse> GetSchemeAsync(ulong id, CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("scheme", await GetOrThrowAsync(id, cancellationToken));

    public async Task<LaravelApiResponse> GetOptionsAsync(CancellationToken cancellationToken) =>
        LaravelApiResponse.Success("options", await _repository.GetOptionsAsync(cancellationToken));

    public async Task<LaravelApiResponse> GenerateSchemeCodeAsync(string? schemeName, string? schemeTag, string? basedOn, CancellationToken cancellationToken)
    {
        var prefix = BuildSchemeCodePrefix(schemeName, schemeTag, basedOn, DateTime.UtcNow.Year);
        var lastCode = await _repository.GetLastSchemeCodeAsync(prefix, cancellationToken);
        var nextSequence = LastSequence(lastCode, prefix) + 1;

        if (nextSequence <= 1 && string.IsNullOrWhiteSpace(lastCode))
        {
            nextSequence = Random.Shared.Next(1, 100);
        }

        var code = $"{prefix}-{nextSequence:00}";
        while (await _repository.SchemeCodeExistsAsync(code, null, cancellationToken))
        {
            nextSequence++;
            code = $"{prefix}-{nextSequence:00}";
        }

        return LaravelApiResponse.Success("scheme_code", code);
    }

    public async Task<LaravelApiResponse> CreateSchemeAsync(LoyaltySchemeRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw Http(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        await ValidateRequestAsync(request, null, true, cancellationToken);
        var schemeCode = string.IsNullOrWhiteSpace(request.SchemeCode)
            ? await GenerateUniqueSchemeCodeAsync(request.SchemeName, request.SchemeTag, request.BasedOn, cancellationToken)
            : request.SchemeCode.Trim().ToUpperInvariant();

        var now = DateTime.UtcNow;
        var scheme = new LoyaltyScheme
        {
            Active = NormalizeActive(request.Active),
            SchemeName = request.SchemeName!.Trim(),
            SchemeCode = schemeCode,
            SchemeDescription = NormalizeText(request.SchemeDescription),
            SchemeTag = NormalizeChoice(request.SchemeTag, "Regular", SchemeTags),
            CustomerType = NormalizeChoice(request.CustomerType, string.Empty, CustomerTypes),
            AreaScope = NormalizeChoice(request.AreaScope, "All", AreaScopes),
            AreaValues = SerializeAreaValues(request.AreaScope, request.AreaValues),
            StartDate = request.StartDate!.Value,
            EndDate = request.EndDate!.Value,
            SchemeType = "Invoice",
            BasedOn = NormalizeChoice(request.BasedOn, "Value", BasedOnOptions),
            Status = NormalizeChoice(request.Status, "Draft", StatusOptions),
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now,
            Slabs = MapSlabs(request.Slabs, now)
        };

        var created = await _repository.CreateSchemeAsync(scheme, cancellationToken);
        return LaravelApiResponse.Success("scheme", created, "Scheme created successfully");
    }

    public async Task<LaravelApiResponse> UpdateSchemeAsync(ulong id, LoyaltySchemeRequestDto request, ulong? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue) throw Http(LaravelStatusCodes.Unauthorized, "Unauthenticated.");
        var scheme = await FindOrThrowAsync(id, cancellationToken);
        await ValidateRequestAsync(request, id, true, cancellationToken);

        var now = DateTime.UtcNow;
        scheme.Active = NormalizeActive(request.Active);
        scheme.SchemeName = request.SchemeName!.Trim();
        scheme.SchemeDescription = NormalizeText(request.SchemeDescription);
        scheme.SchemeTag = NormalizeChoice(request.SchemeTag, "Regular", SchemeTags);
        scheme.CustomerType = NormalizeChoice(request.CustomerType, string.Empty, CustomerTypes);
        scheme.AreaScope = NormalizeChoice(request.AreaScope, "All", AreaScopes);
        scheme.AreaValues = SerializeAreaValues(request.AreaScope, request.AreaValues);
        scheme.StartDate = request.StartDate!.Value;
        scheme.EndDate = request.EndDate!.Value;
        scheme.SchemeType = "Invoice";
        scheme.BasedOn = NormalizeChoice(request.BasedOn, "Value", BasedOnOptions);
        scheme.Status = NormalizeChoice(request.Status, "Draft", StatusOptions);
        scheme.UpdatedBy = actorUserId;
        scheme.UpdatedAt = now;
        scheme.Slabs.Clear();
        foreach (var slab in MapSlabs(request.Slabs, now))
        {
            scheme.Slabs.Add(slab);
        }

        var updated = await _repository.SaveSchemeAsync(scheme, cancellationToken);
        return LaravelApiResponse.Success("scheme", updated, "Scheme updated successfully");
    }

    public async Task<LaravelApiResponse> DeleteSchemeAsync(ulong id, CancellationToken cancellationToken)
    {
        var scheme = await FindOrThrowAsync(id, cancellationToken);
        await _repository.DeleteSchemeAsync(scheme, cancellationToken);
        return LaravelApiResponse.MessageOnly("success", "Scheme deleted successfully");
    }

    private async Task ValidateRequestAsync(LoyaltySchemeRequestDto request, ulong? exceptId, bool allowBlankCode, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.SchemeName)) errors["scheme_name"] = ["Scheme name is required."];
        if (!allowBlankCode && string.IsNullOrWhiteSpace(request.SchemeCode)) errors["scheme_code"] = ["Scheme code is required."];
        if (!request.StartDate.HasValue) errors["start_date"] = ["Start date is required."];
        if (!request.EndDate.HasValue) errors["end_date"] = ["End date is required."];
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value < request.StartDate.Value) errors["end_date"] = ["End date must be after start date."];
        AddChoiceError(errors, "scheme_tag", request.SchemeTag, "Regular", SchemeTags, "Invalid scheme tag.");
        AddChoiceError(errors, "customer_type", request.CustomerType, string.Empty, CustomerTypes, "Invalid customer type.");
        AddChoiceError(errors, "area_scope", request.AreaScope, "All", AreaScopes, "Invalid area scope.");
        AddChoiceError(errors, "based_on", request.BasedOn, "Value", BasedOnOptions, "Invalid based on value.");
        AddChoiceError(errors, "status", request.Status, "Draft", StatusOptions, "Invalid status.");

        if (!string.IsNullOrWhiteSpace(request.SchemeType) && !string.Equals(request.SchemeType.Trim(), "Invoice", StringComparison.OrdinalIgnoreCase))
        {
            errors["scheme_type"] = ["Only Invoice scheme type is currently supported."];
        }

        var areaScope = NormalizeChoice(request.AreaScope, "All", AreaScopes);
        if (!string.Equals(areaScope, "All", StringComparison.OrdinalIgnoreCase) && (request.AreaValues is null || request.AreaValues.Length == 0))
        {
            errors["area_values"] = ["Select at least one area value."];
        }

        if (request.Slabs.Count == 0)
        {
            errors["slabs"] = ["At least one slab is required."];
        }
        else
        {
            for (var index = 0; index < request.Slabs.Count; index++)
            {
                var slab = request.Slabs[index];
                var prefix = $"slabs.{index}";
                if (string.IsNullOrWhiteSpace(slab.TierName)) errors[$"{prefix}.tier_name"] = ["Tier name is required."];
                if (!slab.ValueFrom.HasValue || slab.ValueFrom.Value < 0) errors[$"{prefix}.value_from"] = ["Value from is required and cannot be negative."];
                if (slab.ValueTo.HasValue && slab.ValueFrom.HasValue && slab.ValueTo.Value < slab.ValueFrom.Value) errors[$"{prefix}.value_to"] = ["Value to must be greater than or equal to value from."];
                if (!slab.RewardValue.HasValue || slab.RewardValue.Value < 0) errors[$"{prefix}.reward_value"] = ["Reward value is required and cannot be negative."];
            }
        }

        if (errors.Count > 0) throw Http(LaravelStatusCodes.NoContentLikeValidation, errors);

        if (!string.IsNullOrWhiteSpace(request.SchemeCode)
            && await _repository.SchemeCodeExistsAsync(request.SchemeCode.Trim().ToUpperInvariant(), exceptId, cancellationToken))
        {
            throw Http(LaravelStatusCodes.NoContentLikeValidation, new { scheme_code = new[] { "This scheme code already exists." } });
        }
    }

    private static List<LoyaltySchemeSlab> MapSlabs(IEnumerable<LoyaltySchemeSlabRequestDto> slabs, DateTime now) =>
        slabs.Select((slab, index) => new LoyaltySchemeSlab
        {
            TierName = slab.TierName!.Trim(),
            ValueFrom = slab.ValueFrom!.Value,
            ValueTo = slab.ValueTo,
            RewardValue = slab.RewardValue!.Value,
            SortOrder = index + 1,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

    private async Task<LoyaltySchemeDto> GetOrThrowAsync(ulong id, CancellationToken cancellationToken) =>
        await _repository.GetSchemeAsync(id, cancellationToken) ?? throw Http(LaravelStatusCodes.NotFound, "Scheme not found");

    private async Task<LoyaltyScheme> FindOrThrowAsync(ulong id, CancellationToken cancellationToken) =>
        await _repository.FindSchemeEntityAsync(id, cancellationToken) ?? throw Http(LaravelStatusCodes.NotFound, "Scheme not found");

    private static string SerializeAreaValues(string? areaScope, string[]? areaValues)
    {
        var scope = NormalizeChoice(areaScope, "All", AreaScopes);
        if (string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase)) return "[]";

        var values = (areaValues ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private async Task<string> GenerateUniqueSchemeCodeAsync(string? schemeName, string? schemeTag, string? basedOn, CancellationToken cancellationToken)
    {
        var prefix = BuildSchemeCodePrefix(schemeName, schemeTag, basedOn, DateTime.UtcNow.Year);
        var lastCode = await _repository.GetLastSchemeCodeAsync(prefix, cancellationToken);
        var nextSequence = LastSequence(lastCode, prefix) + 1;

        if (nextSequence <= 1 && string.IsNullOrWhiteSpace(lastCode))
        {
            nextSequence = Random.Shared.Next(1, 100);
        }

        var code = $"{prefix}-{nextSequence:00}";
        while (await _repository.SchemeCodeExistsAsync(code, null, cancellationToken))
        {
            nextSequence++;
            code = $"{prefix}-{nextSequence:00}";
        }

        return code;
    }

    private static string BuildSchemeCodePrefix(string? schemeName, string? schemeTag, string? basedOn, int year)
    {
        var tagPart = string.Equals(schemeTag, "Booster", StringComparison.OrdinalIgnoreCase) ? "BST" : "REG";
        var namePart = Abbr(schemeName);
        var basisPart = string.Equals(basedOn, "Percentage", StringComparison.OrdinalIgnoreCase) ? "PCT" : "VAL";
        return $"{tagPart}-{namePart}-INV-{basisPart}-{year}".ToUpperInvariant();
    }

    private static string Abbr(string? value)
    {
        var clean = new string((value ?? "Scheme").Select(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ').ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(clean)) return "SCH";

        var words = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(3).ToArray();
        var abbreviation = string.Concat(words.Select(word => word[0]));
        if (abbreviation.Length == 0) abbreviation = clean[0].ToString();
        return abbreviation.PadRight(3, abbreviation[0]).Length > 5 ? abbreviation[..5] : abbreviation.PadRight(3, abbreviation[0]);
    }

    private static int LastSequence(string? code, string prefix)
    {
        if (string.IsNullOrWhiteSpace(code) || !code.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase)) return 0;
        var suffix = code[(prefix.Length + 1)..];
        return int.TryParse(suffix, out var sequence) ? sequence : 0;
    }

    private static void AddChoiceError(IDictionary<string, string[]> errors, string key, string? value, string fallback, string[] allowed, string message)
    {
        var normalized = NormalizeChoice(value, fallback, allowed);
        if (string.IsNullOrWhiteSpace(normalized)) errors[key] = [message];
    }

    private static string NormalizeChoice(string? value, string fallback, string[] allowed)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return allowed.FirstOrDefault(x => string.Equals(x, candidate, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private static string NormalizeActive(string? value) =>
        string.Equals(value, "N", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LaravelHttpException Http(int statusCode, object message) => new(statusCode, message);
}
