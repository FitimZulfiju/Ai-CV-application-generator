# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
<!-- Last workflow trigger: 2026-01-30T01:32 -->

## [Unreleased]

---
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
