# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-28

### Added

- Initial open source release
- Multi-provider AI support (OpenAI, Gemini, Claude, Groq, DeepSeek, OpenRouter)
- Job URL scraping from major job boards
- CV and Cover Letter generation
- PDF, HTML, Markdown, JSON export formats
- Multi-language support (English, Albanian, Danish)
- Docker deployment with PostgreSQL and SQL Server support
- Cloudflare Tunnel integration for secure remote access
- OAuth authentication (Google, Microsoft, GitHub)
- Watchtower auto-update support

---

### Recent Changes

- feat: make database initialization safe for open source using reflection
- feat: prepare for open source release v1.0.0
- feat: implement Google Keep-style Notes feature with Masonry layout and full CRUD
- feat: Full localization & Regex optimization
- feat(Generate): Add profile picture toggle for tailored CV
- feat(ci): implement comprehensive Docker Hub tag and digest pruning keeper only latest + 3 versions total
- feat: rename clear draft to clear draft and reload, add project section description, and fix build warnings
- feat: Add Tagline field to Profile, UI, and PDF with localization
- feat: Adjust AdminDashboard usage trends chart styling
- feat(localization): implement full Danish (da) support and translation
- feat: Add email generation feature
- feat: replace JavaScript update banner with native Blazor component
- feat: move beta warning to centered chip in app bar
- feat: change update countdown to 3 minutes (checking interval remains 5 minutes)
- feat: server-side update scheduling - countdown never resets once started
- feat: resolve and display semantic version tag for pending updates
- feat(ui): polish update UX, suppress blazor errors, robust auto-reload
- feat: increase update warning to 5 minutes
- feat: implement controlled update flow with pre-update warning and Watchtower API integration
- feat: add 512x512 application pwa icon
- feat: implement persistent auto-save and refresh mechanisms
- feat: Admin Dashboard, Onboarding, Refactoring & Fixes
- feat: add PWA support by introducing service workers, manifest, and icons
- feat: Implement core UI for AI CV and application generation, profile management, and PWA support.
- feat: add MyApplications page to display, view, and delete user-generated job applications with search and filtering.
- feat: Add unit tests for JobApplicationOrchestrator service.
- feat: enable AI-powered CV profile management, application generation, and PDF export
- feat: refactoring
- feat: implementing external logins
- feat: refactoring (removed .env from tracking)
- feat: implement login page with MudBlazor UI, form fields, password toggle, and error display.
- feat: refactoring.
- feat: refactoring
- feat: refactoring
- feat: refactoring
- feat: Implement PdfService for generating styled CVs and cover letters from candidate profiles using QuestPDF.
- feat: implement PDF generation service for CVs and cover letters using QuestPDF
- feat: adjusting the pdf preview.
- feat: Add PdfService to generate CVs and cover letters using QuestPDF.
- feat: Introduce core project structure for WebCV application and implement PDF generation for CVs and cover letters.
- feat: Add PDF generation service for CVs and cover letters, and introduce components for web-based CV preview and PDF print preview.
- feat: added questPdf.
- feat: add / diplay the job description link in the job description tab
- feat: added print preview button in saved applications.
- feat: add user custom prompt field and finalize cloud-only architecture
- feat: integrate DeepSeek-V3 and cloud AI providers with encrypted API key storage
- feat: Add AI model availability service with caching for cloud and local models, and centralize common usings for the infrastructure project.
- feat: Implement local AI (Ollama) support with GPU acceleration, model selection in generation and user settings, and deployment scripts.
- feat: optimize local AI performance with aggressive context/prediction limits and improve CI/CD versioning debug output
- feat: add CI/CD workflow for automated build, test, and Docker image publishing with semantic versioning
- feat: add CI/CD workflow for building, testing, semantic versioning, and Docker image publishing
- feat: add CI/CD pipeline with Docker build/push and implement local AI service for resume and cover letter generation
- feat: add GitHub Actions CI/CD pipeline for build, test, Docker image push, and semantic versioning.
- feat: add LocalAIService for generating cover letters and tailoring resumes using local AI (Ollama).
- feat: add GitHub Actions CI/CD pipeline and Docker Compose deployment configuration
- feat: Implement AI-powered job application generation and orchestration, including new service factories, orchestrator, and a dedicated UI page.
- feat: Initialize Blazor application with core services, SQL database, Identity authentication, and API endpoints.
- feat: Implement AI service factory and configure core application infrastructure, including authentication, authorization, and database setup.
- feat: introduce AI model management, AI-powered application generation, and a new deployment script
- feat: add GitHub Actions CI/CD workflow for building, testing, and Docker image publishing with semantic versioning.
- feat: Introduce local AI model support via Ollama, including new DTOs, service integration, database migration for AI model enum, and Docker deployment configurations.
- feat: Add local AI (Ollama) integration, enhance AI model selection and configuration, and update deployment.
- feat: Implement user settings management for AI API keys and default model selection, integrating with the generation process.
- feat: add print styles and dynamic font scaling for CVs and cover letters
- feat: Implement print-specific styling for cover letters and dynamic font scaling for CVs and cover letters to optimize content fit.
- feat: add dynamic content scaling for CVs and cover letters with print-specific styling and integrate them into the application layout.
- feat: Implement AI prompt builder for cover letters and resumes, and add dynamic content scaling for document previews.
- feat: add AI-powered application generation page with dynamic CV scaling for print optimization
- feat: Add JavaScript for dynamic CV content scaling to fit content within page boundaries.
- feat: Implement CV profile management page with data entry, a dynamic preview, print-specific styling, and content scaling.
- feat: Implement initial Home page and MainLayout with MudBlazor components and loading indicator.
- feat: test
- feat:test
- feat: enable versioning and fix deployment scripts
- feat: Implement initial application layout and home page using MudBlazor, including new branding assets.
- feat: Add initial deployment infrastructure including Docker, Cloudflare Tunnel, and web application entry point.
- feat: add initial App.razor for base Blazor application layout and styling

### Fixed

- fix(Generate): Fix profile picture toggle not updating CV preview
- fix: some css fix
- fix: some css fix
- fix: some css fixes.
- fix(settings): refine auto-naming and model selection binding
- fix(ai-config): finalize persistence and implement strict provider validation
- fix: resolve auto-refresh and scrollbar issues in update banner
- fix: automate update trigger via background service and remove UI dependency
- fix: Resolved DbUpdateException by nulling User navigation property before profile save
- fix: Prevent skills UI duplication
- fix: Skills duplication, update detection, and reset-database script
- fix: Skills duplication and update detection issues
- fix: Fix UpdateBanner HttpClient base URI for Server-Side Blazor
- fix: Update tests for email generation feature
- fix: add periodic banner restoration check every 2s to handle Blazor navigation
- fix: wait for version change before reload, increase timeout to 90s
- fix: show countdown immediately on refresh if update is scheduled
- fix: reload immediately when server responds, trust cooldown to prevent loops
- fix: add 60-second cooldown after page load before checking updates
- fix: simplify reload logic and add 60s timeout with manual refresh fallback
- fix: return secondsRemaining from scheduleUpdateOnServer for countdown persistence
- fix: prevent reload loop by checking version changed and no pending update
- fix: restore corrupted auto-refresh.js logic and bump to 1.0.6
- fix: resolve variable scope issue in auto-refresh.js and bump to 1.0.4
- fix: unblock UI and allow reconnect modal, bump to 1.0.3
- fix: add cache busting to JS and correct countdown text to 3 minutes
- fix: implement relative countdown logic to solve clock skew issues
- fix: correct update countdown logic and add local fallback in auto-refresh.js
- fix: fix update countdown stalling, move banner to bottom, and add auto-refresh logic
- fix: restore external logins, disable update checks in Dev, and optimize UpdateCheckService (.NET 9 Lock, optimized logging)
- fix: refactoring mobile view
- fix: refactoring the app.razor by removing the markuostring for script tag
- fix(perf): wrap debug/info/error logs in IsEnabled checks to resolve CA1873
- fix: disable watchtower polling entirely, set app check to 5m
- fix: increase watchtower poll to 1h to favor app trigger, fix UI subText bug
- fix: restore periodic polling and remove manual restart button to prevent race conditions
- fix: refactoring the deployment scripts
- fix(ui): Align Profile details and standardize System Logs layout
- fix: small refactoring
- fix: fixing the background drawer
- fix: fixing the drawer
- fix: styling
- fix: fix pdf print preview styling
- fix: fix styling
- fix: fix pdf  print preview styling
- fix: fix styling..
- fix: fix duplicate 'sincerely'
- fix: fix styling.
- fix: fix styling.
- fix: fix styling
- fix: styling.
- fix: fix styling.
- fix: fix the styling.
- fix: fix styling.
- fix: the local models avaliability info message.
- fix: correct semantic versioning patterns to use proper regex format with forward slashes

## [0.17.1] - 2026-01-27

### Changed

- docs: remove screenshot placeholder from README
- docs: update changelog for v0.17.0

## [0.17.0] - 2026-01-27

### Added

- feat: prepare for open source release v1.0.0

<!-- 
This changelog is automatically updated by GitHub Actions on each release.
Commits following Conventional Commits format (feat:, fix:, docs:, etc.) 
will be categorized automatically.
-->
