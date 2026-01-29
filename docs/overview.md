# Self-Hosted AI Job Application Generator

Generate tailored CVs and cover letters from your profile and job postings using a **self-hosted, privacy-first AI system**. Designed to save time while applying for jobs, this application can be run locally or hosted online for multiple users.

---

## Table of Contents

1. [Why This Exists](#why-this-exists)  
2. [Features](#features)  
3. [Supported AI Providers](#supported-ai-providers)  
4. [Inputs](#inputs)  
5. [Outputs](#outputs)  
6. [Execution Model](#execution-model)  
7. [Non-Goals & Limitations](#non-goals--limitations)  
8. [Deployment](#deployment)  
9. [Privacy & Data Handling](#privacy--data-handling)  
10. [Roadmap](#roadmap)  

---

## Why This Exists

Applying for jobs is repetitive and time-consuming. Tailoring CVs and cover letters for each position often takes hours. Many SaaS tools require uploading sensitive personal data, creating privacy concerns.  

This system is self-hosted and **user-controlled**, enabling multiple users or organizations to streamline their application process while keeping all data local.

---

## Features

- Web-based interface for creating and managing user profiles  
- Multi-user support with optional third-party login (Google, Microsoft, GitHub)  
- User profile includes:
  - Personal details
  - Work experience
  - Education
  - Skills
  - Projects
  - Languages
  - Interests
- AI configuration per user:
  - Add multiple providers and models
  - API keys stored encrypted
  - Select model for each generation  
- Job ingestion:
  - Paste job posting URL for scraping
  - Manual job description input if scraping fails  
- Interactive generation:
  - Generates CV and cover letter based on user profile and job description
  - Preview in HTML and plain text
- Outputs:
  - Save/download PDF
  - Export as Markdown or JSON
- Session management:
  - Prevents data loss during system updates
  - 3-minute warning banner for updates  

---

## Supported AI Providers

- OpenAI  
- Google Gemini  
- OpenRouter  
- Grok  
- Deepseek  
- Cloude  

**Notes:**  

- Users provide their own API keys.  
- The system fetches all available models from the selected provider.  
- Structured profile data is sent to AI for generation.  
- Some models may use data for training — users should choose providers accordingly.

---

## Inputs

- **Candidate data:** Data entered in the user profile (personal details, experience, skills, etc.)  
- **Job data:** Job posting URL (scraped) or manually pasted description  

---

## Outputs

- **Tailored CV and cover letter**  
- Preview as PDF in UI  
- Downloadable as PDF  
- Exportable as Markdown or JSON  

---

## Execution Model

- Web UI interaction for all tasks  
- Users can paste a job URL or enter job description manually  
- Interactive, per-job generation  
- Session persistence ensures no data loss during updates  

---

## Non-Goals & Limitations

- Not a job board, ATS, or recruiter  
- Does not guarantee interview success  
- Does not automatically apply to jobs (only generates example plaintext emails)  
- Job scraping may fail on some sites; manual input supported  
- Designed to **save time while applying for jobs**, not replace human decision-making  

---

## Deployment

- Distributed as a **Docker image**  
- Recommended deployment via **Docker Compose**:
  - Blazor application container  
  - SQL Server database container  
  - Watchtower container for automatic updates  
- Optional shell script automates deployment and can create a Cloudflare Tunnel  
- Can be run locally or hosted online for multiple users  

---

## Privacy & Data Handling

See [docs/privacy.md](docs/privacy.md) for details on API key encryption, AI request handling, and local data ownership.

---

## Roadmap

- Enhanced prompt customization  
- Multi-profile support  
- Additional AI provider integrations  
- Improved UI/UX  
- Template customization for CVs and cover letters  

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.
