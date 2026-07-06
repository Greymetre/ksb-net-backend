using System.Data;
using System.Text.Json;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class FieldKonnectMobileController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public FieldKonnectMobileController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpGet("get-field-connet-version")]
    [HttpGet("fieldkonnect/version")]
    [HttpGet("field-konnect/version")]
    public async Task<IActionResult> GetVersion(CancellationToken cancellationToken)
    {
        var setting = await ReadLatestSetting(cancellationToken);
        var media = setting is null
            ? Array.Empty<object>()
            : await ReadSettingMedia(setting.Id, cancellationToken);

        return Ok(new
        {
            status = "success",
            message = "Data retrieved successfully.",
            data = new
            {
                app_version = setting?.AppVersion ?? string.Empty,
                android_version = setting?.AppVersion ?? string.Empty,
                ios_version = setting?.AppIosVersion ?? string.Empty,
                media
            }
        });
    }

    [AllowAnonymous]
    [HttpGet("getAppVersion")]
    [HttpGet("fieldkonnect/app-version")]
    [HttpGet("field-konnect/app-version")]
    public async Task<IActionResult> GetAppVersion(CancellationToken cancellationToken)
    {
        var setting = await ReadLatestSetting(cancellationToken);
        if (setting is null)
        {
            return NotFound(new { status = "error", message = "Settings not found." });
        }

        return Ok(new
        {
            status = "success",
            data = new
            {
                android_version = setting.AppVersion ?? string.Empty,
                ios_version = setting.AppIosVersion ?? string.Empty
            }
        });
    }

    [AllowAnonymous]
    [HttpGet("getsettings")]
    [HttpGet("fieldkonnect/settings")]
    [HttpGet("field-konnect/settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var setting = await ReadLatestSetting(cancellationToken);
        return Ok(new
        {
            status = "success",
            data = new
            {
                app_version = setting?.AppVersion ?? string.Empty,
                android_version = setting?.AppVersion ?? string.Empty,
                ios_version = setting?.AppIosVersion ?? string.Empty,
                order_discount_limit = setting?.OrderDiscountLimit
            }
        });
    }

    [Authorize]
    [AcceptVerbs("GET", "POST")]
    [Route("getOrderDiscountLimit")]
    [Route("fieldkonnect/order-discount-limit")]
    [Route("field-konnect/order-discount-limit")]
    public async Task<IActionResult> GetOrderDiscountLimit(CancellationToken cancellationToken)
    {
        var setting = await ReadLatestSetting(cancellationToken);
        return Ok(new
        {
            status = "success",
            order_discount_limit = setting?.OrderDiscountLimit
        });
    }

    private async Task<FieldKonnectSetting?> ReadLatestSetting(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        await EnsureOpen(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, app_version, order_discount_limit, created_at, updated_at FROM field_konnect_app_settings ORDER BY id DESC LIMIT 1";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var setting = new FieldKonnectSetting
        {
            Id = ReadUInt64(reader, "id"),
            AppVersion = ReadString(reader, "app_version"),
            OrderDiscountLimit = ReadNullableInt32(reader, "order_discount_limit"),
            CreatedAt = ReadNullableDateTime(reader, "created_at"),
            UpdatedAt = ReadNullableDateTime(reader, "updated_at")
        };

        await reader.CloseAsync();
        setting.AppIosVersion = await ReadOptionalIosVersion(setting.Id, cancellationToken);
        return setting;
    }

    private async Task<string?> ReadOptionalIosVersion(ulong settingId, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        await EnsureOpen(connection, cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT app_ios_version FROM field_konnect_app_settings WHERE id = @id LIMIT 1";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@id";
            parameter.Value = settingId;
            command.Parameters.Add(parameter);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value is DBNull or null ? null : Convert.ToString(value);
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<object>> ReadSettingMedia(ulong settingId, CancellationToken cancellationToken)
    {
        var modelTypes = new[]
        {
            "App\\Models\\FieldKonnectAppSetting",
            "App\\\\Models\\\\FieldKonnectAppSetting"
        };

        var mediaRows = await _dbContext.Media
            .Where(media => media.ModelId == settingId
                && modelTypes.Contains(media.ModelType)
                && media.CollectionName == "product_catalogue")
            .OrderBy(media => media.OrderColumn)
            .ThenBy(media => media.Id)
            .ToListAsync(cancellationToken);

        return mediaRows
            .Select(media => new
            {
                id = media.Id,
                model_type = media.ModelType,
                model_id = media.ModelId,
                uuid = media.Uuid,
                collection_name = media.CollectionName,
                name = media.Name,
                file_name = media.FileName,
                mime_type = media.MimeType,
                disk = media.Disk,
                size = media.Size,
                manipulations = ParseJson(media.Manipulations),
                custom_properties = ParseJson(media.CustomProperties),
                generated_conversions = ParseJson(media.GeneratedConversions),
                responsive_images = ParseJson(media.ResponsiveImages),
                order_column = media.OrderColumn,
                created_at = media.CreatedAt,
                updated_at = media.UpdatedAt,
                original_url = $"/storage/{settingId}/{media.FileName}"
            })
            .Cast<object>()
            .ToList();
    }

    private static async Task EnsureOpen(System.Data.Common.DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static ulong ReadUInt64(IDataRecord record, string name)
    {
        var ordinal = record.GetOrdinal(name);
        return record.IsDBNull(ordinal) ? 0 : Convert.ToUInt64(record.GetValue(ordinal));
    }

    private static int? ReadNullableInt32(IDataRecord record, string name)
    {
        var ordinal = record.GetOrdinal(name);
        return record.IsDBNull(ordinal) ? null : Convert.ToInt32(record.GetValue(ordinal));
    }

    private static string? ReadString(IDataRecord record, string name)
    {
        var ordinal = record.GetOrdinal(name);
        return record.IsDBNull(ordinal) ? null : Convert.ToString(record.GetValue(ordinal));
    }

    private static DateTime? ReadNullableDateTime(IDataRecord record, string name)
    {
        var ordinal = record.GetOrdinal(name);
        return record.IsDBNull(ordinal) ? null : Convert.ToDateTime(record.GetValue(ordinal));
    }

    private static object ParseJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Array.Empty<object>();

        try
        {
            return JsonSerializer.Deserialize<object>(value) ?? Array.Empty<object>();
        }
        catch
        {
            return value;
        }
    }

    private sealed class FieldKonnectSetting
    {
        public ulong Id { get; init; }
        public string? AppVersion { get; init; }
        public string? AppIosVersion { get; set; }
        public int? OrderDiscountLimit { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
