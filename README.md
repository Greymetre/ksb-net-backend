# KSB PR ASP.NET Core Backend Migration

This backend is the .NET 8 migration target for the existing Laravel project in `c:\xampp\htdocs\ksb-pr`.

## Laravel Architecture Understanding

The Laravel app is a Laravel 9 monolith with a large mobile/API surface and an admin web surface.

- API routes live in `routes/api.php` and are mounted under `/api`.
- Public API routes include `login`, `signup`, customer auth, product/address dropdowns, Exotel, SAP stock import, app version, and distributor helper endpoints.
- Protected API routes use Passport guards:
  - `auth:customers` for customer app flows.
  - `auth:users,customers` for shared field-user/customer APIs.
  - Some nested routes additionally force `auth:users`.
- Admin web routes in `routes/web.php` are session-authenticated and heavily permission-gated with Spatie permissions/Gates.
- Authentication uses Laravel Passport tokens, not Sanctum, with separate providers for `users` and `customers`.
- Roles and permissions use Spatie tables: `roles`, `permissions`, `model_has_roles`, `model_has_permissions`, and `role_has_permissions`.
- Responses are mostly plain Laravel JSON envelopes like `{ "status": "success", "userinfo": ... }` or `{ "status": "error", "message": ... }`.
- Validation failures often return non-standard HTTP `402`, so this migration preserves that behavior where observed.
- Uploads are mixed: direct `public/uploads/...`, Laravel storage paths, S3 helper usage, and Spatie MediaLibrary.
- `php artisan route:list` currently fails without Exotel credentials because `ExotelService` throws during resolution. The .NET migration avoids startup failure for missing optional third-party keys.

## Current .NET Implementation

Implemented foundation:

- Clean architecture solution under `src/Api`, `src/Application`, `src/Domain`, `src/Infrastructure`, and `src/Shared`.
- ASP.NET Core Web API on .NET 8.
- EF Core with MySQL via Pomelo.
- JWT bearer auth with token revocation checked against `oauth_access_tokens`.
- Swagger with bearer token support.
- Repository and service pattern for auth.
- Laravel-style exception middleware and JSON response envelope.
- Initial entities/mappings for:
  - `users`
  - `customers`
  - `mobile_user_login_details`
  - `oauth_access_tokens`
  - Spatie permission tables
- Initial EF migration: `src/Infrastructure/Migrations/InitialLaravelAuthFoundation`.
- First API endpoints:
  - `POST /api/login`
  - `POST /api/signup`
  - `POST /api/customerSignup`
  - `GET|POST /api/logout`
  - `GET|POST /api/customer/logout`
  - `GET /api/migration-status`

## Run

```powershell
cd c:\xampp\htdocs\ksb-pr\netProject\backend
dotnet restore
dotnet build
dotnet run --project src\Api\Api.csproj --urls http://127.0.0.1:5088
```

Swagger:

```text
http://127.0.0.1:5088/swagger
```

Health/status:

```text
http://127.0.0.1:5088/api/migration-status
```

## Configuration

Set values in `src/Api/appsettings.json` or `src/Api/appsettings.Development.json`.

Important keys:

- `ConnectionStrings:DefaultConnection`
- `Database:MySqlVersion`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Mail`
- `FileUploads`
- `ThirdParty:Exotel`
- `ThirdParty:Firebase`
- `ThirdParty:GoogleMaps`
- `ThirdParty:Infisms`

## Database Migrations

Generate future migrations from the Infrastructure project:

```powershell
dotnet ef migrations add MigrationName --project src\Infrastructure\Infrastructure.csproj --startup-project src\Api\Api.csproj --output-dir Migrations
```

Apply migrations:

```powershell
dotnet ef database update --project src\Infrastructure\Infrastructure.csproj --startup-project src\Api\Api.csproj
```

## Run All Migrations and Seeders

From the backend folder, run the commands in this order:

```powershell
cd c:\xampp\htdocs\ksb-pr\netProject\backend
dotnet restore
dotnet build
dotnet ef database update --project src\Infrastructure\Infrastructure.csproj --startup-project src\Api\Api.csproj
dotnet run --project src\Api\Api.csproj -- --seed-master-data
dotnet run --project src\Api\Api.csproj -- --seed-superadmin
```

`dotnet ef database update` applies all pending EF Core migrations to the configured database. The seeder commands use the same connection string as the API from `src/Api/appsettings.json`, `src/Api/appsettings.Development.json`, or the `KSB_PR_CONNECTION` environment variable.

For design-time migration generation without editing appsettings:

```powershell
$env:KSB_PR_CONNECTION="Server=127.0.0.1;Port=3306;Database=netksb_new;User=root;Password=;"
```

## Migration Order

Continue implementation in this order:

1. Complete authentication parity: `customerLogin`, `customer/email-login`, OTP verification, profile, password reset/email verification if required by the active app.
2. Users module.
3. Roles and permissions.
4. Dashboard.
5. Customers.
6. HR modules: attendance, leave, expenses, appraisal, joining/resignation.
7. Remaining modules endpoint-by-endpoint from `routes/api.php`, then admin web/AJAX routes if those must also move.

## Production Notes

- Replace the development JWT key before deployment.
- Move third-party secrets out of source-controlled JSON into environment variables or a secret store.
- The current AutoMapper NuGet package reports advisory `NU1903`; review or pin to a fixed release before production.
- Do not switch response envelopes or status codes during module migration, because the mobile app depends on Laravel’s current shapes.
