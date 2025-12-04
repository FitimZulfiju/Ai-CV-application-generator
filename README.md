# WebCV Application Generator

A powerful Blazor Server application designed to help job seekers generate tailored CVs and Cover Letters using advanced AI (OpenAI GPT-4o and Google Gemini).

## 🚀 Features

-   **AI-Powered Generation**: Automatically generates professional cover letters and tailored resumes based on your profile and a specific job posting.
-   **Job Post Scraping**: Simply paste a job URL (LinkedIn, Indeed, etc.) to automatically extract job details.
-   **Multi-User Support**: Secure user accounts with ASP.NET Core Identity.
-   **Secure API Key Management**: Users can securely store their own OpenAI and Google Gemini API keys (encrypted in the database).
-   **Application Management**: Save, view, and manage your generated applications in a dedicated dashboard.
-   **Modern UI**: Built with MudBlazor for a responsive and professional user experience.

## 🛠️ Tech Stack

-   **Framework**: .NET 10 (Blazor Server)
-   **UI Library**: MudBlazor
-   **Database**: SQL Server with Entity Framework Core
-   **AI Integration**: OpenAI API & Google Gemini API
-   **Authentication**: ASP.NET Core Identity

## 🔑 Default Admin Credentials

Use these credentials to log in and test the application:

-   **Email:** `admin@webcv.com`
-   **Password:** `Admin123!`

## 🏁 Getting Started

### Prerequisites

-   [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.

### Installation & Run

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/yourusername/WebCV.git
    cd WebCV
    ```

2.  **Build the solution**:
    ```bash
    dotnet build WebCV.sln
    ```

3.  **Run the application**:
    ```bash
    cd WebCV.Web
    dotnet run
    ```

4.  **Open in Browser**:
    Navigate to `https://localhost:7153` (or the URL shown in the console).

### Usage Guide

1.  **Register/Login**: Create an account or use the default admin credentials.
2.  **Profile**: Fill in your candidate profile (Experience, Education, Skills).
3.  **Settings**: Go to the Settings page and enter your OpenAI or Google Gemini API Key.
4.  **Generate**:
    -   Paste a Job URL and click "Fetch".
    -   Select your AI Provider.
    -   Click "Generate Application".
5.  **Save**: Review the generated content and click "Save Application" to store it.
6.  **My Applications**: View and manage your saved applications.

## 📄 License

This project is licensed under the MIT License.
