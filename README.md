# AiCV Application Generator

🚀 **AiCV** is a powerful AI career assistant built with Blazor Server & .NET 10. Automatically generate tailored CVs and Cover Letters optimized for specific job postings using various Large Language Models (LLMs).

AiCV Application Generator is a self-hosted platform designed to streamline the job application process. It uses Large Language Models to analyze job descriptions (scraped directly from URLs) and re-align your professional profile to meet specific role requirements.

## 🚀 Features

- **AI-Powered Generation**: Automatically generates professional cover letters and tailored resumes based on your profile and a specific job posting.
- **Job Post Scraping**: Simply paste a job URL (LinkedIn, Indeed, etc.) to automatically extract job details.
- **Multi-User Support**: Secure user accounts with ASP.NET Core Identity.
- **Secure API Key Management**: Users can securely store their own OpenAI and Google Gemini API keys (encrypted in the database).
- **Application Management**: Save, view, and manage your generated applications in a dedicated dashboard.
- **Modern UI**: Built with MudBlazor for a responsive and professional user experience.

## 🛠️ Tech Stack

- **Framework**: .NET 10 (Blazor Server)
- **UI Library**: MudBlazor
- **Database**: SQL Server with Entity Framework Core
- **AI Integration**: Support for multiple AI Providers (OpenAI, Google Gemini, Groq, etc.)
- **Authentication**: ASP.NET Core Identity

## 🔑 Default Admin Credentials

Use these credentials to log in and test the application:

- **Email:** `testuser@ai-aicv.com`
- **Password:** `TestUser123!`

## 🐳 Docker Hub

The official image is available on Docker Hub: [timi74/aicv](https://hub.docker.com/r/timi74/aicv)

This image provides a pre-configured Blazor Server environment for the AiCV platform. It is designed for privacy-conscious users who want a self-hosted, professional tool for career management.

**Quick Start:**
Deploy using our provided Docker Compose stack to get a fully secured application generator with automated updates and certificate management.

## 🏁 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.

### Installation & Run

1. **Clone the repository**:

    ```bash
    git clone https://github.com/yourusername/AiCV.git
    cd AiCV
    ```

2. **Build the solution**:

    ```bash
    dotnet build AiCV.sln
    ```

3. **Run the application**:

    ```bash
    cd AiCV.Web
    dotnet run
    ```

4. **Open in Browser**:
    Navigate to `https://localhost:7153` (or the URL shown in the console).

### Usage Guide

1. **Register/Login**: Create an account or use the default admin credentials.
2. **Profile**: Fill in your candidate profile (Experience, Education, Skills).
3. **Settings**: Go to the Settings page and enter your OpenAI or Google Gemini API Key.
4. **Generate**:
    - Paste a Job URL and click "Fetch".
    - Select your AI Provider.
    - Click "Generate Application".
5. **Save**: Review the generated content and click "Save Application" to store it.
6. **My Applications**: View and manage your saved applications.

## 📄 License

This project is licensed under the MIT License.
