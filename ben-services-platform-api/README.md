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

## Angular integration
Frontend now calls this API via:
- `src/app/core/config/api.config.ts`

Default API base URL:
- `http://localhost:5001/api`
