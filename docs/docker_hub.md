# AiCV Application Generator - Docker Hub Description

🚀 **AiCV** is a powerful AI career assistant built with Blazor Server & .NET 10. Automatically generate tailored CVs and Cover Letters optimized for specific job postings using various Large Language Models (LLMs).

## 🌟 Key Features

- **AI-Powered Generation**: Tailored resumes and cover letters using various LLMs.
- **Job URL Scraping**: Integration with popular job boards to extract requirements automatically.
- **Self-Hosted & Private**: Your data stays in your control.
- **Automated Updates**: Built-in support for Watchtower for zero-touch maintenance.

---

## 🚀 Quick Start (Docker Compose)

The easiest way to run AiCV is using Docker Compose. We support both **PostgreSQL** (recommended) and **SQL Server**.

### Option 1: PostgreSQL (Recommended)

Create a `docker-compose.yml` file:

```yaml
services:
  app:
    image: timi74/aicv:latest
    container_name: aicv-app
    restart: unless-stopped
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_PROVIDER=PostgreSQL
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=aicv_db;Username=postgres;Password=YourSecurePassword123!;
    depends_on:
      db:
        condition: service_healthy

  db:
    image: postgres:16-alpine
    container_name: aicv-db
    restart: unless-stopped
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=YourSecurePassword123!
      - POSTGRES_DB=aicv_db
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d aicv_db"]
      interval: 5s
      timeout: 5s
      retries: 10

volumes:
  postgres_data:
```

### Option 2: SQL Server

Create a `docker-compose.yml` file:

```yaml
services:
  app:
    image: timi74/aicv:latest
    container_name: aicv-app
    restart: unless-stopped
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_PROVIDER=SqlServer
      - ConnectionStrings__DefaultConnection=Server=db;Database=AiCV_db;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;
    depends_on:
      db:
        condition: service_healthy

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: aicv-db
    restart: unless-stopped
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourSecurePassword123!
    volumes:
      - mssql_data:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourSecurePassword123!" -C -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 5s
      retries: 10

volumes:
  mssql_data:
```

### Using Environment Variables (Two Approaches)

You can configure AiCV in two ways:

#### Option A: Inline (as shown above)

Hardcode values directly in your `docker-compose.yml` - simple for quick testing.

#### Option B: Using a `.env` file (Recommended for Production)

Create a `.env` file in the same directory as your `docker-compose.yml`:

```ini
# .env file
DB_PROVIDER=PostgreSQL
DB_NAME=aicv_db
DB_USER=postgres
DB_PASSWORD=YourSecurePassword123!
```

Then reference variables in your `docker-compose.yml`:

```yaml
services:
  app:
    image: timi74/aicv:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_PROVIDER=${DB_PROVIDER}
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};
    # ...

  db:
    image: postgres:16-alpine
    environment:
      - POSTGRES_USER=${DB_USER}
      - POSTGRES_PASSWORD=${DB_PASSWORD}
      - POSTGRES_DB=${DB_NAME}
    # ...
```

### Environment Variables Reference

| Variable | Description | Default |
| --- | --- | --- |
| `DB_PROVIDER` | Database provider: `SqlServer` or `PostgreSQL` | `SqlServer` |
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` for deployment | `Production` |
| `DB_NAME`, `DB_USER`, `DB_PASSWORD` | Shared database settings | - |
| `ConnectionStrings__DefaultConnection` | Full connection string (recommended) | - |

---

## 🔗 Links

- **Docker Hub**: [timi74/aicv](https://hub.docker.com/r/timi74/aicv)
- **GitHub Repository**: [FitimZulfiju/Ai-CV-application-generator](https://github.com/FitimZulfiju/Ai-CV-application-generator)
- **LinkedIn**: [Fitim Zulfiju](https://linkedin.com/in/[your-profile])
- **Support**: <[your-email@example.com]>
