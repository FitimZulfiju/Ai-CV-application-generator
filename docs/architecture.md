# Architecture Overview — Self-Hosted AI Job Application Generator

This document describes the high-level architecture of the system, including major components, data flow, and key design decisions.

---

## 1. System Overview

- Blazor web application with SQL database backend  
- Enables users to create/manage profiles, configure AI models, and generate CVs & cover letters  
- Supports multiple users; can be self-hosted locally or online  
- Goals: privacy-first, multi-user, flexible AI provider integration  

---

## 2. High-Level Components

### Blazor Web UI

- User authentication (local + OAuth)
- Profile management (personal details, experience, skills)
- Settings page for API keys and model selection
- Job ingestion and generation interface
- Output preview and export
- Session management and update notifications

### SQL Database

- Stores users, encrypted API keys, profiles, generated documents
- Persists active sessions

### AI Orchestration Layer

- Receives generation requests
- Selects provider and model per user
- Sends structured profile data (no raw CVs) to AI
- Returns JSON outputs for rendering

### Job Scraper / Input Processor

- Scrapes job postings or accepts manual input
- Normalizes text for AI consumption

### Output Renderer

- Renders CV and cover letter in:
  - HTML preview
  - Plain text
  - PDF
  - Markdown
  - JSON
- Applies predefined templates

### Deployment / DevOps Components

- Docker Compose setup:
  - Blazor app container
  - SQL Server container
  - Watchtower container
- Optional Cloudflare Tunnel for secure remote access
- Networking managed by Docker Compose

---

## 3. Data Flow

1. Users fill profile → saved in SQL  
2. Job URL → Scraper extracts text OR manual input → normalized  
3. User selects AI provider/model → AI Orchestration sends structured request → receives JSON response  
4. Output Renderer formats CV & cover letter → preview & export  
5. Optional storage in SQL for reuse

---

## 4. Security & Privacy

- API keys encrypted at rest  
- Only structured profile data sent to AI  
- No raw personal documents transmitted  
- User chooses providers/models  

---

## 5. Multi-User Support

- Supports multiple concurrent users  
- Isolated profiles and generated documents  
- OAuth login (Google, Microsoft, GitHub)  
- Session persistence prevents data loss  

---

## 6. Optional Features

- Multi-provider/model support  
- Watchtower automatic updates  
- Template customization  
- Cloudflare Tunnel automation  

---

## 7. Conceptual Diagram

```mermaid
graph TD
    User[User] -->|HTTPS| Cloudflare[Cloudflare Tunnel / Proxy]
    Cloudflare -->|HTTP| WebUI[Blazor Web UI (Container)]
    
    subgraph "Docker Host"
        WebUI -->|Read/Write| SQL[SQL Database (Container)]
        WebUI -->|Scrape| JobSites[External Job Sites]
        WebUI -->|API Call| AI[AI Providers (OpenAI, Gemini, etc.)]
        
        FileSys[File System]
        WebUI -->|Persist Keys/Logs| FileSys
        SQL -->|Persist Data| FileSys
    end

    subgraph "External Services"
        AI
        JobSites
        OAuth[OAuth Providers (Google, MS, GitHub)]
        WebUI -->|Auth| OAuth
    end
```
