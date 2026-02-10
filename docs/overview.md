# AiCV — Self-Hosted AI Job Application Generator

Generate tailored CVs and cover letters using a **self-hosted, privacy-first AI system**. Designed to save time applying for jobs, it can run locally or be hosted online for multiple users.

---

## Table of Contents

1. [Purpose](#purpose)
2. [Features](#features)
3. [Supported AI Providers](#supported-ai-providers)
4. [Inputs](#inputs)
5. [Outputs](#outputs)
6. [Execution Model](#execution-model)
7. [Limitations](#limitations)
8. [Deployment](#deployment)
9. [Privacy & Data Handling](#privacy--data-handling)
10. [Roadmap](#roadmap)

---

## Purpose

Tailoring CVs and cover letters for each job is repetitive and time-consuming. Many SaaS solutions require uploading personal data, raising privacy concerns. **AiCV** is self-hosted and user-controlled, allowing organizations or individuals to streamline applications while keeping all data local.

---

## Features

* Web interface for creating/managing user profiles
* Multi-user support with optional OAuth (Google, Microsoft, GitHub)
* User profile includes:

  * Personal details
  * Work experience
  * Education
  * Skills
  * Projects
  * Languages
  * Interests
* AI configuration per user:

  * Multiple providers and models
  * API keys encrypted at rest
  * Model selection per generation
* Job ingestion:

  * Paste job URL for scraping
  * Manual input if scraping fails
* Interactive generation:

  * Generates CV and cover letter from profile and job description
  * Preview in HTML and plain text
* Outputs:

  * Save/download as PDF
  * Export as Markdown or JSON
* Session management:

  * Prevents data loss during updates
  * Update warning banner (3 minutes)

---

## Supported AI Providers

* OpenAI
* Google Gemini
* OpenRouter
* Groq
* Deepseek
* Claude

**Notes:**

* Users provide API keys.
* System fetches available models per provider.
* Only structured profile data is sent to AI.
* Some models may use data for training — choose providers carefully.

---

## Inputs

* **Candidate data:** From user profile (personal details, experience, skills, etc.)
* **Job data:** Job posting URL (scraped) or manual description

---

## Outputs

* Tailored CV and cover letter
* Preview as PDF in UI
* Downloadable PDF
* Exportable as Markdown or JSON

---

## Execution Model

* Web UI for all interactions
* Paste job URL or enter description manually
* Interactive per-job generation
* Session persistence ensures no data loss during updates

---

## Limitations

* Not a job board, ATS, or recruiter
* No guarantee of interview success
* Does not apply to jobs automatically (plaintext email examples only)
* Job scraping may fail; manual input supported
* Designed to save time, not replace human decision-making

---

## Deployment

* Distributed as a **Docker image**
* Recommended deployment via **Docker Compose**:

  * Blazor app container
  * SQL Server/PostgreSQL database container
  * Watchtower container for automatic updates
* Optional shell scripts automate deployment and Cloudflare Tunnel setup
* Can run locally or online for multiple users

---

## Privacy & Data Handling

See [docs/privacy.md](docs/privacy.md) for details on API key encryption, AI request handling, and local data ownership.

---

## Roadmap

* Enhanced prompt customization
* Multi-profile support
* Additional AI provider integrations
* Improved UI/UX
* CV and cover letter template customization

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.
