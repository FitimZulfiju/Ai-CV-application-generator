# AI-CV Application Architecture

## Overview
This document outlines the architecture and organization of the AI-CV project. The project is built with .NET 10.0 and Blazor Server, following a clean architecture-inspired structure.

## Project Structure

### `AiCV.Domain`
- Contains core entities and domain logic.
- No dependencies on other projects.
- Entities: `User`, `CandidateProfile`, `WorkExperience`, `Education`, `Skill`, `Project`, etc.

### `AiCV.Application`
- Contains business logic, interfaces, and DTOs.
- `Interfaces`: Define service contracts (`ICVService`, `IPdfService`, etc.).
- `Common`: Shared utilities like `CvUtils`.

### `AiCV.Infrastructure`
- Implementation of external services and data access.
- `Data`: `ApplicationDbContext` and Migrations (via separate migration projects).
- `Services`:
    - `PdfService`: Orchestrates PDF generation.
    - `PdfTemplates`: Design-specific template classes (`ProfessionalTemplate`, `ModernTemplate`, `MinimalistTemplate`) implementing `ICvPdfTemplate`.
    - `UpdateCheckService`: Background service for monitoring Docker Hub for updates.
- `Extensions`: Modular service registrations (e.g., `InfrastructureServiceCollectionExtensions`).

### `AiCV.Web`
- The Blazor Server application (UI).
- `Components`:
    - `Pages`: Routed pages (`Home.razor`, `Profile.razor`).
    - `Shared`: Reusable UI components.
    - `Shared/ProfileSections`: Extracted sections from the large Profile page for better maintainability.
- `Extensions`: Web-specific service and middleware configurations (`ServiceCollectionExtensions`, `WebApplicationExtensions`).
- `Models`: UI-specific view models.

## Key Design Patterns

### Modular Startup
The `Program.cs` is kept lean by delegating configuration to extension methods in the `AiCV.Web.Extensions` and `AiCV.Infrastructure.Extensions` namespaces.

### Template Method / Strategy for PDF Generation
PDF designs are encapsulated in separate template classes implementing `ICvPdfTemplate`. `PdfService` selects the appropriate template at runtime.

### Componentized UI
Large Razor pages (like `Profile.razor`) are broken down into smaller, focused components (e.g., `WorkExperienceSection.razor`) to improve readability and limit file size.

## Database Strategy
The application supports both **PostgreSQL** and **SQL Server**.
- Provider is selected via the `DB_PROVIDER` environment variable.
- Connection strings can be provided via standard `DefaultConnection` or specific env vars (`PG_HOST`, `DB_SERVER`, etc.).
- Migrations are isolated into `AiCV.Migrations.PostgreSQL` and `AiCV.Migrations.SqlServer` projects.
