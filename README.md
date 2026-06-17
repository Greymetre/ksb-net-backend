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

## Mobile Location Lookup API

Use this master API on customer registration/profile screens where city, pincode, and state can autofill each other. The existing state dropdown remains:

```text
GET /api/masters/states?search=Delhi
```

Use the location lookup endpoint when the customer types or selects any one location field:

```text
GET /api/masters/location-lookup?pincode=110001
GET /api/masters/location-lookup?city=Delhi
GET /api/masters/location-lookup?city_id=1
GET /api/masters/location-lookup?state_id=1
```

Alias:

```text
GET /api/masters/locations?pincode=110001
```

Example response when customer enters pincode:

```json
{
  "status": "success",
  "locations": [
    {
      "country": {
        "id": 1,
        "country_name": "India",
        "active": "Y"
      },
      "state": {
        "id": 10,
        "state_name": "Delhi",
        "country_id": 1,
        "country_name": "India",
        "gst_code": "07",
        "active": "Y"
      },
      "district": {
        "id": 110,
        "district_name": "Central Delhi",
        "state_id": 10,
        "state_name": "Delhi",
        "active": "Y"
      },
      "city": {
        "id": 1001,
        "city_name": "Delhi",
        "district_id": 110,
        "district_name": "Central Delhi",
        "state_id": 10,
        "state_name": "Delhi",
        "active": "Y"
      },
      "pincodes": [
        {
          "id": 50001,
          "pincode": "110001",
          "city_id": 1001,
          "city_name": "Delhi",
          "active": "Y"
        }
      ]
    }
  ]
}
```

Example response when customer selects a state:

```json
{
  "status": "success",
  "locations": [
    {
      "state": {
        "id": 10,
        "state_name": "Delhi",
        "country_id": 1,
        "country_name": "India",
        "active": "Y"
      },
      "city": {
        "id": 1001,
        "city_name": "Delhi",
        "state_id": 10,
        "state_name": "Delhi",
        "active": "Y"
      },
      "pincodes": [
        {
          "id": 50001,
          "pincode": "110001",
          "city_id": 1001,
          "active": "Y"
        },
        {
          "id": 50002,
          "pincode": "110002",
          "city_id": 1001,
          "active": "Y"
        }
      ]
    },
    {
      "state": {
        "id": 10,
        "state_name": "Delhi",
        "country_id": 1,
        "country_name": "India",
        "active": "Y"
      },
      "city": {
        "id": 1002,
        "city_name": "New Delhi",
        "state_id": 10,
        "state_name": "Delhi",
        "active": "Y"
      },
      "pincodes": [
        {
          "id": 50003,
          "pincode": "110003",
          "city_id": 1002,
          "active": "Y"
        }
      ]
    }
  ]
}
```

Mobile handling:

- If the user types pincode, call with `pincode`; if one location returns, autofill city and state.
- If the user types city, call with `city`; show returned city options because city names can repeat across states.
- If the user selects state from `/masters/states`, call with `state_id`; show cities and pincodes for that state.
- Keep city, state, and pincode editable after autofill. When the user changes one field, call the lookup again using the latest field.
- Store `state.id`, `city.id`, and selected `pincodes[].id` when available; display `state_name`, `city_name`, and `pincode` to the user.

## Mobile My Invoices List API

Use this API for the read-only mobile invoice list screen. It supports search, status filter, date filter, pagination, and month grouping.

```text
GET /api/invoices?search=INV-2847&status=approved&fromDate=2026-04-01&toDate=2026-05-31&page=1&pageSize=20
```

Query parameters:

- `search`: optional invoice number/shop search text.
- `status`: optional `all`, `approved`, `pending`, `rejected`; old labels such as `Approved By HO` also work.
- `fromDate` / `toDate`: optional date range.
- `page` / `pageSize`: pagination.

Example response:

```json
{
  "status": "success",
  "summary": {
    "total_invoices": 9,
    "rewards_credited": 14300,
    "rewards_credited_display": "₹14,300",
    "approved_invoices": 8,
    "pending_invoices": 1,
    "rejected_invoices": 0,
    "total_turnover": 750000,
    "total_turnover_display": "₹7,50,000"
  },
  "filter_options": {
    "search_placeholder": "Search invoice number",
    "statuses": [
      { "key": "all", "label": "All" },
      { "key": "approved", "label": "Approved" },
      { "key": "pending", "label": "Pending" },
      { "key": "rejected", "label": "Rejected" }
    ]
  },
  "groups": [
    {
      "month_key": "2026-05",
      "month_label": "MAY 2026",
      "count": 3,
      "turnover": 430000,
      "turnover_display": "₹4,30,000",
      "reward_amount": 7250,
      "reward_display": "+₹7,250",
      "items": [
        {
          "id": 2847,
          "invoice_number": "INV-2847",
          "invoice_number_display": "#INV-2847",
          "invoice_date": "2026-05-28T00:00:00",
          "display_date": "28 May, 2:30 PM",
          "month_key": "2026-05",
          "month_label": "MAY 2026",
          "amount": 200000,
          "amount_display": "₹2,00,000",
          "reward_amount": 4000,
          "reward_display": "+₹4,000",
          "reward_label": "Reward",
          "status": "approved",
          "status_label": "Approved By HO",
          "is_reward_credited": true,
          "is_pending": false,
          "attachment": "/uploads/invoices/inv-2847.pdf",
          "scheme_name": "Retailer Monthly Slab",
          "scheme_names": ["Retailer Monthly Slab"]
        }
      ]
    },
    {
      "month_key": "2026-04",
      "month_label": "APRIL 2026",
      "count": 3,
      "turnover": 320000,
      "turnover_display": "₹3,20,000",
      "reward_amount": 4800,
      "reward_display": "+₹4,800",
      "items": [
        {
          "id": 2771,
          "invoice_number": "INV-2771",
          "invoice_number_display": "#INV-2771",
          "display_date": "20 Apr, 12:00 PM",
          "amount": 0,
          "amount_display": "₹0",
          "reward_amount": 0,
          "reward_display": null,
          "reward_label": "Awaiting approval",
          "status": "pending",
          "status_label": "Pending",
          "is_reward_credited": false,
          "is_pending": true
        }
      ]
    }
  ],
  "items": [],
  "data": [],
  "pagination": {
    "page": 1,
    "page_size": 20,
    "total": 9
  }
}
```

Mobile handling:

- Show `summary.total_invoices` and `summary.rewards_credited_display` in the top cards.
- Render `groups` by `month_label`; show month `count`, `turnover_display`, and `reward_display`.
- Render each invoice from `groups[].items`.
- For approved rows, show `reward_display` and `reward_label`.
- For pending rows, show a dash in the amount/right column and use `reward_label` as the subtitle.
- Use `items` only if the app wants a flat list. `data` is kept for backward compatibility with the older raw response.
- Reward points are created only after HO approval. Pending, SS-approved, Sales-approved, and rejected invoices return `reward_amount: 0`, `reward_display: null`, and `scheme_points: 0` in the raw `data` rows.
- Slab/point calculation uses only the total amount of HO-approved invoices in the scheme period. Non-HO invoices do not count toward slab turnover or customer point totals.

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

## Docker and Railway Deploy

Railway runs database setup through `preDeployCommand` before starting the API container:

```sh
/app/db-bootstrap.sh
```

The bootstrap script runs migrations, master-data seed, and superadmin seed. Railway is configured by `railway.json` to build with the backend `Dockerfile`. Set one of these database configurations in Railway:

- `KSB_PR_CONNECTION` with a full MySQL connection string.
- Railway MySQL variables such as `MYSQL_URL`, `DATABASE_URL`, or `MYSQLHOST`/`MYSQLDATABASE`/`MYSQLUSER`/`MYSQLPASSWORD`/`MYSQLPORT`.

Set `SKIP_DB_BOOTSTRAP=true` only if you need to deploy without running migrations and seeders.

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
