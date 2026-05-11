# Ben Services Platform API (.NET + MySQL)

## Stack
- ASP.NET Core Web API (.NET 8)
- Entity Framework Core 9
- Pomelo MySQL provider

## Project path
- `ben-services-platform-api`

## Connection string
Configured in:
- `appsettings.json`
- `appsettings.Development.json`

Current value:
- Server: `127.0.0.1`
- Port: `3306`
- Database: `benservices`
- User: `manager`
- Password: `manager`

## Run
```bash
dotnet restore
dotnet run
```

## Migrations
Generated migration:
- `Migrations/20260508233646_InitialCreate.cs`

SQL script export:
- `migrations/initial-create.sql`
- `migrations/create-benservices.sql` (includes `CREATE DATABASE` + `USE benservices`)

Apply migrations:
```bash
dotnet ef database update
```

### Aiven sync (recommended)
Use this flow to bring an existing Aiven MySQL database up to the exact schema in this repo.

1. Generate an idempotent script from all EF migrations:
```bash
dotnet ef migrations script --idempotent --output migrations/aiven-sync.sql
```

2. Apply it to Aiven (safe to re-run):
```bash
mysql --ssl-mode=REQUIRED --user <AIVEN_USER> --password=<AIVEN_PASSWORD> --host <AIVEN_HOST> --port <AIVEN_PORT> <AIVEN_DB_NAME> < migrations/aiven-sync.sql
```

3. Confirm applied migrations:
```sql
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
```

Current expected migrations in production are:
- `20260508233646_InitialCreate`
- `20260510040658_AddProviderComplianceDocuments`
- `20260510150257_AddAdminAuthentication`

## Production environment variables
Set these in Render (or your host) before deploying:

- `ConnectionStrings__BenServicesDatabase`
- `JWT_SECRET` (at least 32 chars)
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_EXPIRATION_MINUTES` (for example `120`)
- `CORS_ALLOWED_ORIGINS` (comma-separated frontend URLs)

### First admin bootstrap
If the `admins` table is empty, startup now expects bootstrap values in non-development environments:

- `DEFAULT_ADMIN_EMAIL`
- `DEFAULT_ADMIN_PASSWORD` (must satisfy password policy)
- `DEFAULT_ADMIN_FULLNAME`

On first start, the API creates this admin and marks `MustChangePassword = true`.

## API routes
- `GET /api/providers`
- `GET /api/providers/{id}`
- `POST /api/providers`
- `PUT /api/providers/{id}`
- `POST /api/providers/{id}/verify`
- `POST /api/providers/{id}/deactivate`
- `DELETE /api/providers/{id}`

- `GET /api/applications`
- `GET /api/applications/{id}`
- `POST /api/applications`
- `POST /api/applications/{id}/approve`
- `POST /api/applications/{id}/reject`
- `POST /api/applications/{id}/request-more-info`

- `POST /api/auth/login`
- `GET /api/auth/me`
- `POST /api/auth/change-password`
- `GET /api/admins`
- `POST /api/admins`

## Angular integration
Frontend now calls this API via:
- `src/app/core/config/api.config.ts`

Default API base URL:
- `http://localhost:5001/api`
