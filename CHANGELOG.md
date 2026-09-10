# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
<!-- Last workflow trigger: 2026-01-30T01:32 -->

## [Unreleased]

### Added
- feat: add customizable CV section titles and Unicode icons
- feat: add `<chip>` / `<badge>` custom tags for inline pill-shaped badges in CV text fields (rendered in both browser preview and PDF)
- feat: add `<s>` / `<del>` strikethrough support in PDF documents
- feat: add `<mark>` highlight support in PDF documents (bold + dark-gold text)
- feat: add `<sub>` / `<sup>` subscript/superscript support in PDF documents
- feat: extend named CSS color dictionary from 17 to 65 entries for PDF rendering

### Fixed
- fix: `<br>` tags now correctly produce visual line breaks in generated PDF documents
- fix: `font-style: italic` from inline `style=` attributes is now applied in PDF documents
- fix: `font-weight: 600` and `900` values now correctly trigger bold in PDF documents

---
## [1.17.3] - 2026-09-10

### Changed
- Add Immediate property to Description field in ExperienceTab for instant updates

## [1.17.2] - 2026-09-10

### Changed
- Allow header text to fill available space in Modern and Professional PDF templates when photo is shown

## [1.17.1] - 2026-09-10

### Changed
- Fix chip text alignment and size in PDF generation to match HTML preview

## [1.17.0] - 2026-09-10

### Added
- feat: Add support for u, s, mark, and chip tags in PDF and HTML generation, fix Windows line breaks and text alignment

### Fixed
- fix: Update Description field in ExperienceTab for improved styling and layout

## [1.16.1] - 2026-08-12

### Fixed
- fix: Make Watchtower HTTP API URL configurable via env var

## [1.16.0] - 2026-08-12

### Added
- feat: add application icons and favicon to web assets

## [1.15.3] - 2026-08-05

### Changed
- Update packages, add profile photo toggle in saved applications view, and resolve ReverseMarkdown obsolete warnings

## [1.15.2] - 2026-07-03

### Fixed
- fix: increase watchtower HTTP request timeout to 10 minutes to prevent premature cancellation during image pull

## [1.15.1] - 2026-07-03

### Changed
- Enhance Application Email generation and formatting

## [1.15.0] - 2026-06-17

### Added
- feat: Add localized MudBlazor error pages, responsive UI fixes, and service improvements

### Fixed
- fix(services): correct DangerousUnprotect usage and remove nullable init to satisfy analyzers
- fix: avoid FormatException when decoding non-base64 API keys in Unprotect
- fix: hide chip help alert on mobile; remove vertical gap in SectionHeaderCard on xs

## [1.14.1] - 2026-06-16

### Changed
- chore: Apply manual UI refinements across pages and components

## [1.14.0] - 2026-06-16

### Added
- feat: Add Generate page draft persistence, Profile Cancel button, SectionHeaderCard component, and fix chip gap
- feat: Add Core Competencies chip view and arrow-based chip reordering

## [1.13.1] - 2026-06-14

### Changed
- Refactor Skills to CoreCompetencies, fix footer markdown formatting, and sync EF migrations

## [1.13.0] - 2026-06-14

### Added
- feat: Add LogCleanupBackgroundService for automatic system logs retention management

## [1.12.9] - 2026-06-14

### Changed
- Refactor: Enhance CV date calculation and simplify UI date pickers

## [1.12.8] - 2026-06-13

### Fixed
- fix: apply CSS isolation fix for Languages and Interests chip alignment
- fix: overhaul SkillsTab layout with independent masonry columns and drag-and-drop improvements

## [1.12.7] - 2026-06-13

### Changed
- chore: revert AGENTS.md tracking and add to gitignore
- chore: enforce strict AI operational rules via AGENTS.md

## [1.12.6] - 2026-06-11

### Changed
- Update breadcrumb separator to ChevronRight icon

## [1.12.5] - 2026-06-11

### Changed
- Fix Watchtower HTTP API trigger method (POST) and add self-healing on failure

## [1.12.4] - 2026-06-11

### Fixed
- fix: force https scheme in production for OAuth and feat: remove duplicate deploy directory

## [1.12.2] - 2026-06-11

### Changed
- Potential fix for code scanning alert no. 7: Workflow does not contain permissions
- security: untaint password reset token and email body to resolve CodeQL alert #22
- Potential fix for code scanning alert no. 7: Workflow does not contain permissions
- security: remove PII and clear-text token exposure from authentication logs
- Potential fix for pull request finding 'CodeQL / Exposure of private information'

## [1.12.1] - 2026-06-08

### Changed
- chore: consolidate migrations into single InitialSetup

## [1.12.0] - 2026-06-08

### Added
- feat: add Display As Chips layout toggle for languages and interests
- feat: centralize demo credentials and add frictionless demo login UI
- feat: add customizable CV section titles and Unicode icons

### Fixed
- fix: overhaul logging architecture and preserve email on failed login attempts
- fix: use actual CvPreview component for sample CV dialog instead of hardcoded layout

## [1.11.1] - 2026-06-06

### Fixed
- fix: resolve ShellCheck CI failures in deploy scripts

## [1.11.0] - 2026-06-04

### Added
- feat: show version in sidebar footer

## [1.10.1] - 2026-06-04

### Fixed
- fix: keep watchtower update state until restart

## [1.10.0] - 2026-06-04

### Added
- feat: add selectable data backup import export
- feat: add profile json backup controls

## [1.9.4] - 2026-05-28

### Changed
- fix cover letter pdf and html links

## [1.9.3] - 2026-05-07

### Changed
- Handle external account linking and password settings

## [1.9.2] - 2026-05-06

### Changed
- chore: tweak update banner text

## [1.9.1] - 2026-05-06

### Fixed
- fix: prevent updater from getting stuck

## [1.9.0] - 2026-05-06

### Added
- feat: integrate OpenRouter OAuth and unified AiModelPicker component

## [1.8.13] - 2026-04-08

### Changed
- Move routing namespace to global usings
- Add centered desktop breadcrumb to app bar

## [1.8.12] - 2026-04-08

### Changed
- Align app bar menu button spacing
- Refine nav drawer label and scroll-to-top

## [1.8.11] - 2026-04-08

### Changed
- Use global loading on admin pages

## [1.8.10] - 2026-04-08

### Changed
- Ignore local dotnet CLI cache
- Fix responsive app bar and drawer behavior

## [1.8.9] - 2026-04-08

### Changed
- Use mini drawer variant for collapsed nav icons
- Restore button loading indicators on generate page
- Refine layout scrolling and consolidate global loading

## [1.8.8] - 2026-04-07

### Changed
- docs: add small README note for workflow trigger

## [1.8.7] - 2026-04-07

### Changed
- Align release changelog and update timing flow

## [1.8.6] - 2026-04-07

### Changed
- Merge branch 'dev' of https://github.com/FitimZulfiju/Web-CV-application-generator into dev
- Fix Docker publish path and update layout changes
- docs: update changelog for v1.8.6 [skip staging]
- Merge branch 'dev' of https://github.com/FitimZulfiju/Web-CV-application-generator into dev
- Fix release versioning and runtime version display
## [1.8.0] - 2026-04-07

### Added
- feat: enforce production release automation and branch sync flow

### Fixed
- fix: restore PR creation by using GitHub CLI directly
- fix: sync changelog back to dev to prevent divergence

## [1.8.0] - 2026-04-06

### Added
- feat: enforce production release automation and branch sync flow

### Fixed
- fix: restore PR creation by using GitHub CLI directly
- fix: sync changelog back to dev to prevent divergence

## [1.8.0] - 2026-03-19

### Added
- feat: enforce production release automation and branch sync flow

### Fixed
- fix: restore PR creation by using GitHub CLI directly
- fix: sync changelog back to dev to prevent divergence

## [1.8.0] - 2026-03-19

### Added
- feat: enforce production release automation and branch sync flow

### Fixed
- fix: restore PR creation by using GitHub CLI directly
- fix: sync changelog back to dev to prevent divergence

## [1.8.0] - 2026-03-19

### Added
- feat: enforce production release automation and branch sync flow

### Fixed
- fix: restore PR creation by using GitHub CLI directly
- fix: sync changelog back to dev to prevent divergence

## [1.8.0] - 2026-03-17

### Added
- feat: enforce production release automation and branch sync flow

### Fixed
- fix: restore PR creation by using GitHub CLI directly
- fix: sync changelog back to dev to prevent divergence

## [1.7.2] - 2026-03-17

### Changed
- Refactor CV description spacing and bullet point handling to improve compactness and allow manual markdown bullets/checkmarks
## [1.7.0] - 2026-03-17

### Added
- feat: update packages.
- feat: test the new dev-to-master CI/CD review workflow

### Fixed
- fix: align CI/CD versioning logic and ensure production release triggers on master
- fix: resolve LanguageSwitcher production failure by using fingerprinted assets in .NET 9/10 and fixing activator typo
- fix(ci-cd): improve dockerhub cleanup script to delete orphaned digests
- fix: PDF paragraph spacing, NuGet updates, and UI cleanups\n\n- Add ParagraphSpacing to PDF cover letter to match HTML preview gaps\n- Update EF Core packages to 10.0.5 and Npgsql to 10.0.1\n- Remove commented-out beta warning from MainLayout\n- Fix LanguageSwitcher menu activator context and toggle
- fix: fix CI/CD PR creation by consolidating changelog and PR steps

## [1.7.0] - 2026-03-17

### Added
- feat: update packages.
- feat: test the new dev-to-master CI/CD review workflow

### Fixed
- fix: align CI/CD versioning logic and ensure production release triggers on master
- fix: resolve LanguageSwitcher production failure by using fingerprinted assets in .NET 9/10 and fixing activator typo
- fix(ci-cd): improve dockerhub cleanup script to delete orphaned digests
- fix: PDF paragraph spacing, NuGet updates, and UI cleanups
- fix: fix CI/CD PR creation by consolidating changelog and PR steps

## [1.6.4] - 2026-03-09

### Changed
- Merge pull request #43 from FitimZulfiju/master
- chore: merge dev into master and resolve conflicts
- docs: update changelog for v1.6.3 [skip staging]
- chore: finalize dev-to-master workflow with docker cleanup intact
- chore: implement dev-to-master CI/CD workflow
## [1.6.3] - 2026-03-09

### Changed
- Refactor CI/CD to implement Branch-based Staging with Auto-PR and Review flow
- chore: finalize dev-to-master workflow with docker cleanup intact
- chore: implement dev-to-master CI/CD workflow

## [1.6.2] - 2026-03-09

### Changed
- Revert to Pull Request method for changelog to comply with repo rules

## [1.6.0] - 2026-03-09

### Changed
- chore: update projects to .NET 10, fix CV rendering, and harmonize test cancellation
- Merge pull request #40 from FitimZulfiju/automated-changelog-update
- docs: update changelog for v1.5.0

## [1.5.0] - 2026-03-08

### Added
- feat: Add VS Code workspace file for project setup.
- feat: add multi-template support and enhance UI/UX for applications


## [1.4.0] - 2026-02-27

### Added
- feat: profile cv preview tab styling.


## [1.3.5] - 2026-02-27

### Changed
- Remove draft saving, fix strong tag coloring, improve hex color parsing
- Merge pull request #37 from FitimZulfiju/automated-changelog-update
- docs: update changelog for v1.3.4

## [1.3.4] - 2026-02-26

### Changed
- Update README.md
- Merge pull request #36 from FitimZulfiju/automated-changelog-update
- docs: update changelog for v1.3.3

## [1.3.3] - 2026-02-25

### Changed
- Merge branch 'master' of https://github.com/FitimZulfiju/Web-CV-application-generator
- security: remediate CodeQL PII exposure alerts by removing email hashes from logs
- Merge pull request #35 from FitimZulfiju/automated-changelog-update
- docs: update changelog for v1.3.2

## [1.3.2] - 2026-02-25

### Fixed
- fix: resolve build errors and remediate PII exposure in Program.cs


## [1.2.0] - 2026-02-23

### Added
- feat: Implement automated database backup service and database initialization on startup

### Fixed
- fix: Un-ignore and add DbInitializer.cs so CI pipeline can compile it
- fix: Fully qualify DbInitializer in Program.cs to fix CI/CD build error


## [1.1.6] - 2026-01-30

### Changed

- test: verify docker build runs
- Merge pull request #18 from FitimZulfiju/automated-changelog-update
- docs: update changelog for v1.1.5

## [1.1.5] - 2026-01-30

### Fixed

- fix: skip Docker build for changelog commits [skip changelog]
- fix: detect changelog merge commits by branch name [skip changelog]

## [1.1.4] - 2026-01-30

### Changed

- Merge pull request #16 from FitimZulfiju/automated-changelog-update
- docs: update changelog for v1.1.3

## [1.1.3] - 2026-01-30

### Fixed

- fix: prevent infinite loop when changelog PRs are merged

## [1.1.1] - 2026-01-30

### Changed

- Merge pull request #14 from FitimZulfiju/automated-changelog-update
- docs: update changelog for v1.1.0

## [1.1.0] - 2026-01-30

### Added

- Merge pull request #13 from FitimZulfiju/trigger/test-v3
- feat: test workflow trigger v3



## [1.0.5] - 2026-01-28

### Changed

- docs: clean changelog and fix automation anchor

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

<!-- 
This changelog is automatically updated by GitHub Actions on each release.
Commits following Conventional Commits format (feat:, fix:, docs:, etc.) 
will be categorized automatically.
-->
