# AiCV Application Generator - Docker Hub Description

🚀 **AiCV** is a powerful AI career assistant built with Blazor Server & .NET 10. Automatically generate tailored CVs and Cover Letters optimized for specific job postings using various Large Language Models (LLMs).

## 🌟 Key Features

- **AI-Powered Generation**: Tailored resumes and cover letters using various LLMs.
- **Job URL Scraping**: Integration with popular job boards to extract requirements automatically.
- **Self-Hosted & Private**: Your data stays in your control.
- **Automated Updates**: Built-in support for Watchtower for zero-touch maintenance.

---

## 🚀 Quick Start (Docker Compose)

The easiest way to run AiCV is using Docker Compose. Create a `docker-compose.yml` file with the following content:

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
      - ConnectionStrings__DefaultConnection=Server=db;Database=AiCV;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;
    depends_on:
      - db
    labels:
      - "com.centurylinklabs.watchtower.enable=true"
      - "com.centurylinklabs.watchtower.scope=aicv-scope"

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: aicv-db
    restart: unless-stopped
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourSecurePassword123!
    volumes:
      - mssql_data:/var/opt/mssql

  watchtower:
    image: containrrr/watchtower:latest
    container_name: aicv-watchtower
    command: --http-api-update --label-enable --cleanup --scope aicv-scope --interval 300
    restart: unless-stopped
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock

volumes:
  mssql_data:
```

### Environment Variables

| Variable | Description |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` |
| `WATCHTOWER_HTTP_API_TOKEN` | (Optional) Token for manual update triggers |

---

## 🔗 Links

- **GitHub Repository**: [fitimzulfiu/AiCV](https://github.com/fitimzulfiu/Web-CV-application-generator)
- **LinkedIn**: [Fitim Zulfiju](https://linkedin.com/in/[your-profile])
- **Support**: <[your-email@example.com]>
