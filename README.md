# AiCV – AI‑Assisted CV Application Generator

**AiCV** is an open‑source, self‑hosted **experimental .NET application** that explores AI‑assisted workflows for generating CVs and cover letters based on job postings.

The project focuses on:

* LLM provider abstraction in a real‑world .NET application
* Privacy‑first, self‑hosted architecture
* Exploring trade‑offs in UX, data modeling, and AI‑assisted content generation

It is **not a finished product** and **not intended to replace human‑written CVs or cover letters**. Generated content should be treated as a **starting point**, not a final submission.

---

## ⚠️ Project Status & Intent

**Status:** Active, early‑stage, evolving

This repository is intentionally public to:

* Track architectural decisions and refactors openly
* Collect early feedback on structure, boundaries, and maintainability
* Experiment with AI integration patterns beyond trivial demos

Parts of the codebase were built rapidly with **heavy AI assistance during early exploration**. Some areas are intentionally left unrefined while architecture and scope are validated.

**Feedback is especially welcome on:**

* Project structure and boundaries
* Database and provider abstraction
* Maintainability and readability

---

## 🌐 Live Demo

[Live Demo](https://aicv.fitim.it.com)

> **Important**
> To use generation features, you must provide your own LLM API key (OpenAI, Gemini, etc.).
> Keys are stored encrypted at rest in the database and can be deleted at any time.

---

## 🚀 Current Capabilities

* **Multi‑Provider AI Support**
  Bring your own API keys for OpenAI, Google Gemini, Claude (Anthropic), Groq, DeepSeek, or OpenRouter.

* **Job Description Ingestion**
  Paste a job URL (LinkedIn, Indeed, etc.) or manually enter details. Basic extraction is used to derive requirements.

* **Privacy‑First by Design**
  All data is stored in your own database. No external tracking or third‑party storage.

* **Authentication & Account Merging**
  Local accounts plus OAuth (Google, Microsoft, GitHub). Automatically merges accounts with matching email addresses.

* **User Account Self-Service**
  Full control over your data. Delete your entire account and all associated AI configurations directly from settings.

* **System Protection**
  Built-in safeguards for default demo accounts and critical system configurations to prevent accidental deletion.

* **Multi-Language UI**
  English, Albanian (Shqip), Danish (Dansk).

* **Export Options**
  PDF generation and Markdown/JSON exports.

---

## 🛠️ Tech Stack

* **Framework:** .NET 10 (Blazor Server)
* **UI:** MudBlazor
* **ORM:** Entity Framework Core
* **PDF Generation:** QuestPDF
* **Database:** SQL Server or PostgreSQL

> **Database note:**
> Both SQL Server and PostgreSQL are currently supported to explore different local and containerized deployment scenarios.
> Migrations are provider‑specific. Consolidation and simplification are tracked in GitHub issues.

---

## 🏁 Getting Started

### Prerequisites

* Docker Desktop (recommended)
* OR .NET 10 SDK + SQL Server/PostgreSQL

### 🐳 Docker (Quick Start)

```yaml
services:
  app:
    image: timi74/aicv:latest
    ports:
      - "8080:80"
    environment:
      - DB_PROVIDER=PostgreSQL
      - PG_HOST=db
      - DB_PASSWORD=YourSecurePassword123!
    depends_on:
      - db
  db:
    image: postgres:16-alpine
    environment:
      - POSTGRES_PASSWORD=YourSecurePassword123!
```

```bash
docker-compose up -d
```

Open: [http://localhost:8080](http://localhost:8080)

---

## 🔧 Local Development

```bash
git clone https://github.com/FitimZulfiju/Web-CV-application-generator.git
cd Web-CV-application-generator
cp .env.example .env
dotnet run --project AiCV.Web
```

Visit: [https://localhost:7153](https://localhost:7153)

---

## 📚 Documentation

* [Project Overview](docs/overview.md)
* [Architecture](docs/architecture.md)
* [Deployment](docs/deployment.md)
* [Privacy](docs/privacy.md)

---

## 🤝 Contributing

This project welcomes constructive feedback and incremental improvements.

If you're interested in:

* Refactoring structure
* Improving boundaries
* Clarifying architecture

Please check existing issues or open a new one with concrete suggestions.

---

## 📄 License

MIT License

---

## 🔗 Links

* [Live Demo](https://aicv.fitim.it.com)
* [Docker Hub](https://hub.docker.com/r/timi74/aicv)
* [GitHub](https://github.com/FitimZulfiju/Web-CV-application-generator)