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
      - PG_HOST=db
      - DB_NAME=aicv_db
      - DB_USER=sa
      - DB_PASSWORD=YourSecurePassword123!
    depends_on:
      - db

  db:
    image: postgres:16-alpine
    container_name: aicv-db
    restart: unless-stopped
    environment:
      - POSTGRES_USER=sa
      - POSTGRES_PASSWORD=YourSecurePassword123!
      - POSTGRES_DB=aicv_db
    volumes:
      - postgres_data:/var/lib/postgresql/data

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
      - DB_SERVER=db
      - DB_NAME=AiCV_db
      - DB_USER=sa
      - DB_PASSWORD=YourSecurePassword123!
    depends_on:
      - db

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: aicv-db
    restart: unless-stopped
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourSecurePassword123!
    volumes:
      - mssql_data:/var/opt/mssql

volumes:
  mssql_data:
```

### Environment Variables

| Variable | Description | Default |
| --- | --- | --- |
| `DB_PROVIDER` | Database provider: `SqlServer` or `PostgreSQL` | `SqlServer` |
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` for deployment | `Production` |
| `DB_NAME`, `DB_USER`, `DB_PASSWORD` | Shared database settings | - |
| `PG_HOST`, `PG_PORT` | PostgreSQL specific host and port | `localhost`, `5432` |
| `DB_SERVER`, `DB_PORT` | SQL Server specific host and port | `localhost`, `1433` |
| `ConnectionStrings__DefaultConnection` | (Alternative) Full connection string (overrides individual variables) | - |

---

## 🔗 Links

- **Docker Hub**: [timi74/aicv](https://hub.docker.com/r/timi74/aicv)
- **GitHub Repository**: [fitimzulfiu/AiCV](https://github.com/fitimzulfiu/Web-CV-application-generator)
- **LinkedIn**: [Fitim Zulfiju](https://linkedin.com/in/[your-profile])
- **Support**: <[your-email@example.com]>
