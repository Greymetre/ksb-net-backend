using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddLaravelCompatibleSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "KSB PR ASP.NET Core API",
                Version = "v1",
                Description = "Laravel-compatible API migration surface"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.OperationFilter<SimpleApiDescriptionFilter>();
        });

        return services;
    }
}

public sealed class SimpleApiDescriptionFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var action = context.ApiDescription.ActionDescriptor.RouteValues["action"] ?? string.Empty;
        var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"] ?? string.Empty;
        var key = $"{controller}.{action}";

        if (Descriptions.TryGetValue(key, out var description))
        {
            operation.Summary = description.Summary;
            operation.Description = description.Details;
        }
        else
        {
            operation.Summary ??= BuildFallbackSummary(context.ApiDescription.HttpMethod, context.ApiDescription.RelativePath);
            operation.Description ??= "Use this endpoint to work with this API resource. Send a Bearer token when the endpoint requires login.";
        }

        if (operation.Parameters is not null)
        {
            foreach (var parameter in operation.Parameters)
            {
                parameter.Description ??= ParameterDescriptions.GetValueOrDefault(parameter.Name);
            }
        }

        if (operation.RequestBody is not null)
        {
            operation.RequestBody.Description ??= BuildRequestBodyDescription(action);
        }
    }

    private static string BuildFallbackSummary(string? method, string? path) =>
        $"{method?.ToUpperInvariant() ?? "API"} {path}";

    private static string BuildRequestBodyDescription(string action) =>
        action switch
        {
            "CreateRole" or "UpdateRole" => "Send role name, optional guard_name, and optional permission IDs.",
            "SyncRolePermissions" => "Send permission IDs that should be assigned to this role. Existing role permissions are replaced.",
            "SaveRolePermissions" => "Send permissions grouped by role ID, matching the Laravel role permission matrix.",
            "CreateBranch" or "UpdateBranch" => "Send branch_name and optional branch_code and active status.",
            "CreateDivision" or "UpdateDivision" => "Send division_name and optional active status.",
            "CreateDesignation" or "UpdateDesignation" => "Send designation_name and optional active status.",
            "CreateDepartment" or "UpdateDepartment" => "Send name and optional active status.",
            "SetBranchActive" or "SetDivisionActive" or "SetDesignationActive" or "SetDepartmentActive" => "Send active as Y or N. If active is omitted, the API toggles the current status.",
            "Login" => "Send username and password. Optional device details can be sent for mobile login tracking.",
            "Signup" => "Send user details for creating an employee/user account.",
            "CustomerSignup" => "Send customer signup details for creating a customer account.",
            "GetLocationDetails" => "Send pincode, state_id, city_id, or city to fetch linked country, state, district, city, and pincode data.",
            _ => "Send the JSON fields required by this endpoint."
        };

    private sealed record ApiDescriptionText(string Summary, string Details);

    private static readonly Dictionary<string, ApiDescriptionText> Descriptions = new()
    {
        ["Auth.Login"] = new(
            "Login user or customer",
            "Checks the username and password, creates a JWT access token, and returns the logged-in user/customer information."),
        ["Auth.Signup"] = new(
            "Create user account",
            "Creates a new user account using the same signup fields used by Laravel. Use this when adding an employee/user from the API."),
        ["Auth.CustomerSignup"] = new(
            "Create customer account",
            "Creates a customer account and returns an access token for customer login flows."),
        ["Auth.Logout"] = new(
            "Logout current account",
            "Revokes the current access token. Works for both user logout and customer logout routes."),

        ["Health.MigrationStatus"] = new(
            "Check API health",
            "Returns basic API health and migration status information."),

        ["MasterData.GetCountry"] = new(
            "List active countries",
            "Returns active countries for dropdowns. Use search to filter by country name."),
        ["MasterData.GetState"] = new(
            "List active states",
            "Returns active states for dropdowns. You can filter by country_id and search text."),
        ["MasterData.GetDistrict"] = new(
            "List active districts",
            "Returns active districts for dropdowns. You can filter by state_id and search text."),
        ["MasterData.GetCity"] = new(
            "List active cities",
            "Returns active cities for dropdowns. You can filter by state_id, district_id, and search text."),
        ["MasterData.GetPincode"] = new(
            "List active pincodes",
            "Returns active pincodes for dropdowns. You can filter by city_id or pincode text."),
        ["MasterData.GetLocationDetails"] = new(
            "Get location details",
            "Returns country, state, district, city, and pincode details by pincode, state_id, city_id, or city name."),

        ["MasterData.GetBranches"] = new(
            "List branches",
            "Returns active HR branches for user create/update forms. Requires the branch permission."),
        ["MasterData.GetBranch"] = new(
            "Get one branch",
            "Returns one branch by ID. Requires the branch permission."),
        ["MasterData.CreateBranch"] = new(
            "Create branch",
            "Creates a new branch in the HR master. Requires the branch permission."),
        ["MasterData.UpdateBranch"] = new(
            "Update branch",
            "Updates branch name/code/warehouse/status. Requires the branch permission."),
        ["MasterData.SetBranchActive"] = new(
            "Change branch status",
            "Sets or toggles branch active status. Send active as Y or N, or omit active to toggle. Requires the branch permission."),
        ["MasterData.DeleteBranch"] = new(
            "Delete branch",
            "Soft deletes a branch and marks it inactive. Requires the branch permission."),

        ["MasterData.GetDivisions"] = new(
            "List divisions",
            "Returns active HR divisions for user create/update forms. Requires the division permission."),
        ["MasterData.GetDivision"] = new(
            "Get one division",
            "Returns one division by ID. Requires the division permission."),
        ["MasterData.CreateDivision"] = new(
            "Create division",
            "Creates a new division in the HR master. Requires the division permission."),
        ["MasterData.UpdateDivision"] = new(
            "Update division",
            "Updates division name/status. Requires the division permission."),
        ["MasterData.SetDivisionActive"] = new(
            "Change division status",
            "Sets or toggles division active status. Send active as Y or N, or omit active to toggle. Requires the division permission."),
        ["MasterData.DeleteDivision"] = new(
            "Delete division",
            "Soft deletes a division and marks it inactive. Requires the division permission."),

        ["MasterData.GetDesignations"] = new(
            "List designations",
            "Returns active HR designations for user create/update forms. Requires the designation permission."),
        ["MasterData.GetDesignation"] = new(
            "Get one designation",
            "Returns one designation by ID. Requires the designation permission."),
        ["MasterData.CreateDesignation"] = new(
            "Create designation",
            "Creates a new designation in the HR master. Requires the designation permission."),
        ["MasterData.UpdateDesignation"] = new(
            "Update designation",
            "Updates designation name/status. Requires the designation permission."),
        ["MasterData.SetDesignationActive"] = new(
            "Change designation status",
            "Sets or toggles designation active status. Send active as Y or N, or omit active to toggle. Requires the designation permission."),
        ["MasterData.DeleteDesignation"] = new(
            "Delete designation",
            "Soft deletes a designation and marks it inactive. Requires the designation permission."),

        ["MasterData.GetDepartments"] = new(
            "List departments",
            "Returns active HR departments for user create/update forms. Requires the departments permission."),
        ["MasterData.GetDepartment"] = new(
            "Get one department",
            "Returns one department by ID. Requires the departments permission."),
        ["MasterData.CreateDepartment"] = new(
            "Create department",
            "Creates a new department in the HR master. Requires the departments permission."),
        ["MasterData.UpdateDepartment"] = new(
            "Update department",
            "Updates department name/status. Requires the departments permission."),
        ["MasterData.SetDepartmentActive"] = new(
            "Change department status",
            "Sets or toggles department active status. Send active as Y or N, or omit active to toggle. Requires the departments permission."),
        ["MasterData.DeleteDepartment"] = new(
            "Delete department",
            "Soft deletes a department and marks it inactive. Requires the departments permission."),

        ["Roles.GetRoles"] = new(
            "List roles",
            "Returns roles for the Role menu. Non-superadmin users do not see role ID 1. Requires role_access."),
        ["Roles.GetRole"] = new(
            "Get one role",
            "Returns one role with its permissions from role_has_permissions. Requires role_access."),
        ["Roles.CreateRole"] = new(
            "Create role",
            "Creates a role and optionally assigns permission IDs to it through role_has_permissions. Requires role_create."),
        ["Roles.UpdateRole"] = new(
            "Update role",
            "Updates role name/guard and optionally replaces its permissions in role_has_permissions. Requires role_edit."),
        ["Roles.DeleteRole"] = new(
            "Delete role",
            "Deletes a role and removes its role_has_permissions and model_has_roles links. Requires role_delete."),
        ["Roles.SyncRolePermissions"] = new(
            "Replace role permissions",
            "Replaces all permissions for one role using role_has_permissions only. Requires role_edit."),
        ["Roles.SaveRolePermissions"] = new(
            "Save role permission matrix",
            "Updates permissions for many roles at once, like the Laravel Role menu checkbox matrix. Requires role_edit."),
        ["Roles.GetPermissions"] = new(
            "List permissions",
            "Returns available permissions for building the Role menu permission matrix. Requires role_access.")
    };

    private static readonly Dictionary<string, string> ParameterDescriptions = new()
    {
        ["id"] = "Database ID of the record.",
        ["search"] = "Optional text used to filter the list.",
        ["country_id"] = "Optional country ID used to filter states.",
        ["state_id"] = "Optional state ID used to filter districts, cities, or location details.",
        ["district_id"] = "Optional district ID used to filter cities.",
        ["city_id"] = "Optional city ID used to filter pincodes or location details.",
        ["pincode"] = "Optional pincode used to filter pincodes or fetch location details.",
        ["city"] = "Optional city name used to fetch location details.",
        ["include_permissions"] = "When true, each role includes its assigned permissions."
    };
}
