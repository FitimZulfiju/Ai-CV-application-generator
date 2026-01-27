# Deployment Guide

This guide covers how to deploy **AiCV** in a production-like environment using the included scripts and Docker Compose.

> **Note**: The scripts in the `deploy/` directory are optimized for an Ubuntu/Debian server environment.

## Quick Start (Production)

We provide a helper script [`deploy/deploy.sh`](../deploy/deploy.sh) that automates:

- Installing Docker & Docker Compose
- Setting up user permissions
- Creating directory structures
- Configuring DNS with Cloudflare Tunnels (optional)
- Starting the application

### Prerequisites

- A Linux server (Ubuntu 22.04+ recommended)
- A domain name (if using Cloudflare Tunnels)
- Cloudflare account & API Token (if using Cloudflare Tunnels)

### 1. Prepare Environment

Create a `.env` file in the `deploy/` directory (you can copy `.env.example` from the root):

```bash
cp .env.example deploy/.env
nano deploy/.env
```

### 2. Prepare Scripts (Important)

If you uploaded scripts from Windows, you must convert them to Unix format and make them executable:

```bash
cd deploy
# Make dos2unix script executable
chmod +x dos2unix.sh
# Run it to fix all other scripts
./dos2unix.sh
```

### 3. Cloudflare Tunnel Setup (Recommended for Public Access)

If you want to expose the app securely using Cloudflare Tunnels:

1. Make sure `cloudflared` is installed and you are logged in (`cloudflared tunnel login`).
2. Run the setup script **BEFORE** the main deployment:

    ```bash
    ./TunnelSetup.sh
    ```

3. This script will:
    - Create a new tunnel (if needed).
    - Update your `.env` file automatically with the new `TUNNEL_ID`.
    - Configure the DNS CNAME records for your domain.

### 4. Run Deployment Script

Once the environment and tunnel are ready, run the main deployment script:

```bash
./deploy.sh
```

The script will:

1. Install Docker & Docker Compose if missing.
2. Create necessary folders (`logs/`, `data/`, `backups/`, `wwwroot/uploads`, `wwwroot/images`, `dataprotection-keys`) at `APP_BASE_DIR`.
3. Set correct permissions.
4. Start the application with persistent volumes.

---

## 💾 Persistent Storage

AiCV is configured to persist all user data outside of the Docker container. The following directories are mapped to your host machine:

- `./logs`: Application and update logs.
- `./dataprotection-keys`: Encryption keys for sensitive data (API keys, etc.).
- `./wwwroot/images`: System-level images.
- `./wwwroot/uploads`: User-uploaded files (like profile pictures).

**Important**: Never delete the `dataprotection-keys` folder, as it contains the keys needed to decrypt your saved AI provider API keys.

---

## 💻 Local Deployment (Windows / Docker Desktop)

You can easily run AiCV locally on Windows using Docker Desktop.

1. **Install Docker Desktop**: Ensure it is running.
2. **Configure Environment**:
    - Copy `.env.example` to the root directory as `.env`.
    - Set `DB_PROVIDER` to `PostgreSQL` (easiest) or `SqlServer`.
3. **Run with Docker Compose**:
    Open PowerShell in the project root and run:

    ```powershell
    docker-compose up -d
    ```

4. **Access App**: Open browser at `http://localhost:8080`.

---

## Manual Deployment

If you prefer not to use the automated script, follow these steps:

### 1. Identify Environment

Decide whether to use **PostgreSQL** or **SQL Server**.

### 2. Choose Compose File

- For PostgreSQL: `deploy/docker-compose.postgres.yml`
- For SQL Server: `deploy/docker-compose.sqlserver.yml`

### 3. Start Application

```bash
docker-compose -f deploy/docker-compose.postgres.yml up -d
```
