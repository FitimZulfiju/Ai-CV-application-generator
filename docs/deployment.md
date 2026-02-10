# Deployment Guide — AiCV

This guide covers how to deploy **AiCV** in a production-like environment using the included scripts and Docker Compose.

> ⚠️ **Note:** This project is experimental. The scripts in the `deploy/` directory are optimized for Ubuntu/Debian servers.

## Quick Start (Production)

We provide a helper script [`deploy/deploy.sh`](../deploy/deploy.sh) that automates:

* Installing Docker & Docker Compose
* Setting up user permissions
* Creating directory structures
* Configuring DNS with Cloudflare Tunnels (optional)
* Starting the application

### Prerequisites

* A Linux server (Ubuntu 22.04+ recommended)
* A domain name (if using Cloudflare Tunnels)
* Cloudflare account & API Token (if using Cloudflare Tunnels)

### 1. Prepare Environment

Create a `.env` file in the `deploy/` directory (copy `.env.example` from the root):

```bash
cp .env.example deploy/.env
nano deploy/.env
```

### 2. Prepare Scripts

If scripts were uploaded from Windows, convert to Unix format and make executable:

```bash
cd deploy
chmod +x dos2unix.sh
./dos2unix.sh
```

### 3. Cloudflare Tunnel Setup (Optional)

1. Install `cloudflared` and log in (`cloudflared tunnel login`).
2. Run the setup script **before** deployment:

```bash
./TunnelSetup.sh
```

This will:

* Create a new tunnel if needed
* Update `.env` with `TUNNEL_ID`
* Configure DNS CNAME records

### 4. Run Deployment Script

```bash
./deploy.sh
```

The script will:

1. Install Docker & Docker Compose if missing
2. Create folders (`logs/`, `data/`, `backups/`, `wwwroot/uploads`, `wwwroot/images`, `dataprotection-keys`) at `APP_BASE_DIR`
3. Set correct permissions
4. Start the application with persistent volumes

## Persistent Storage

Directories mapped to host:

* `./logs`: Application and update logs
* `./dataprotection-keys`: Encryption keys (DO NOT DELETE)
* `./wwwroot/images`: System-level images
* `./wwwroot/uploads`: User-uploaded files

**Important:** `dataprotection-keys` contains keys to decrypt AI API keys.

## Local Deployment (Windows / Docker Desktop)

1. Install Docker Desktop
2. Configure `.env` in project root:

   * `DB_PROVIDER` = `PostgreSQL` (recommended) or `SqlServer`
3. Run:

```powershell
docker-compose up -d
```

4. Access: `http://localhost:8080`

## Manual Deployment

### 1. Identify Environment

* PostgreSQL or SQL Server

### 2. Choose Compose File

* PostgreSQL: `deploy/docker-compose.postgres.yml`
* SQL Server: `deploy/docker-compose.sqlserver.yml`

### 3. Start Application

```bash
docker-compose -f deploy/docker-compose.postgres.yml up -d
```
