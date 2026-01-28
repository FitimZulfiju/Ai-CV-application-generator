#!/bin/bash
set -euo pipefail

# Load variables from .env first (before any other operations)
if [ -f .env ]; then
    set -o allexport
    source .env
    set +o allexport
else
    echo "[ERROR] .env file not found!"
    exit 1
fi

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] [$1] $2"
}

FULL_SUBDOMAIN="$SUBDOMAIN.$DOMAIN"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Get the appropriate docker-compose file based on DB_PROVIDER
get_compose_file() {
    if [ "${DB_PROVIDER:-SqlServer}" = "PostgreSQL" ]; then
        echo "docker-compose.postgres.yml"
    else
        echo "docker-compose.sqlserver.yml"
    fi
}

# Ensure docker group exists
if ! getent group ${DOCKER_GROUP} > /dev/null; then
    echo "[INFO] Docker group does not exist. Creating group '${DOCKER_GROUP}'..."
    sudo groupadd ${DOCKER_GROUP}
fi

# Add user to docker group if not already a member
if ! groups "$USER" | grep -q "\\b${DOCKER_GROUP}\\b"; then
    echo "[INFO] Adding user $USER to ${DOCKER_GROUP} group..."
    sudo usermod -aG ${DOCKER_GROUP} "$USER"
    echo "[INFO] User $USER added to ${DOCKER_GROUP} group. Please log out and log back in."
    exit 1
fi

remove_docker_credsstore() {
    DOCKER_CONFIG_FILE="$HOME/.docker/config.json"
    if [ -f "$DOCKER_CONFIG_FILE" ]; then
        if grep -q '"credsStore"' "$DOCKER_CONFIG_FILE"; then
            log "INFO" "Removing credsStore from Docker config to avoid credential helper errors."
            # Remove the credsStore line (in-place)
            sed -i '/"credsStore"/d' "$DOCKER_CONFIG_FILE"
        fi
    fi
}

install_dependencies() {
    log "INFO" "Forcing APT to use IPv4"
    echo 'Acquire::ForceIPv4 "true";' | sudo tee /etc/apt/apt.conf.d/99force-ipv4

    log "INFO" "Checking and installing required tools..."
    sudo apt-get update -y
    sudo apt-get install -y \
        openssh-server \
        curl \
        jq \
        dos2unix \
        nano \
        ca-certificates \
        apt-transport-https \
        gnupg \
        lsb-release \
        net-tools \
        iputils-ping

    sudo systemctl enable --now ssh

    # Add Docker repository
    log "INFO" "Installing Docker..."
    sudo mkdir -p /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/${OS_DISTRO}/gpg | sudo gpg --yes --dearmor -o /etc/apt/keyrings/docker.gpg

    DISTRO=$(lsb_release -cs)

    if [[ "$DISTRO" == "noble" ]]; then
        # Fallback to jammy repo if noble is not supported
        DISTRO="jammy"
    fi

    echo \
      "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
      https://download.docker.com/linux/${OS_DISTRO} $DISTRO stable" | \
      sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

    sudo apt-get update -y
    sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # Fallback if docker-compose plugin is missing
    if ! command_exists docker-compose; then
        log "WARN" "docker-compose plugin not found, installing standalone binary"
        sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" \
            -o /usr/local/bin/docker-compose
        sudo chmod +x /usr/local/bin/docker-compose
    fi
}

fix_env_file() {
    if [ -f .env ]; then
        log "INFO" "Converting .env to Unix format"
        dos2unix .env
    fi
}

ensure_docker_permissions() {
    log "INFO" "Ensuring user has Docker permissions..."
    sudo usermod -aG ${DOCKER_GROUP} "$USER"
    log "INFO" "User $USER added to the ${DOCKER_GROUP} group. Please log out and back in."
}

check_required_env_vars() {
    REQUIRED_VARS=(CF_API_TOKEN DOMAIN TUNNEL_NAME SUBDOMAIN FULL_SUBDOMAIN)
    for var in "${REQUIRED_VARS[@]}"; do
        if [ -z "${!var:-}" ]; then
            log "ERROR" "Required environment variable $var is not set!"
            exit 1
        fi
    done
}

ensure_dns_record() {
    check_required_env_vars

    log "INFO" "Checking DNS records..."

    ZONE_RESPONSE=$(curl -s -X GET "https://api.cloudflare.com/client/v4/zones?name=$DOMAIN" \
        -H "Authorization: Bearer $CF_API_TOKEN" -H "Content-Type: application/json")

    ZONE_ID=$(echo "$ZONE_RESPONSE" | jq -r '.result[0].id')

    if [ -z "$ZONE_ID" ] || [ "$ZONE_ID" = "null" ]; then
        echo "$ZONE_RESPONSE" | jq .  # Debug
        log "ERROR" "Failed to retrieve ZONE_ID for $DOMAIN"
        exit 1
    fi

    check_and_update_record() {
        local name="$1"
        local target="$TUNNEL_ID.cfargotunnel.com"

        log "INFO" "Checking DNS record for $name..."

        RESPONSE=$(curl -s -X GET "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records?name=$name&type=CNAME" \
            -H "Authorization: Bearer $CF_API_TOKEN" -H "Content-Type: application/json")

        RECORD_ID=$(echo "$RESPONSE" | jq -r '.result[0].id // empty')
        RECORD_CONTENT=$(echo "$RESPONSE" | jq -r '.result[0].content // empty')

        if [ -z "$RECORD_ID" ]; then
            log "INFO" "Creating CNAME DNS record for $name..."
            CREATE_RESPONSE=$(curl -s -X POST "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records" \
                -H "Authorization: Bearer $CF_API_TOKEN" -H "Content-Type: application/json" \
                --data "{\"type\":\"CNAME\",\"name\":\"$name\",\"content\":\"$target\",\"proxied\":true}")
            echo "$CREATE_RESPONSE" | jq .
        elif [ "$RECORD_CONTENT" != "$target" ]; then
            log "INFO" "Updating CNAME record for $name from $RECORD_CONTENT to $target..."
            UPDATE_RESPONSE=$(curl -s -X PUT "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records/$RECORD_ID" \
                -H "Authorization: Bearer $CF_API_TOKEN" -H "Content-Type: application/json" \
                --data "{\"type\":\"CNAME\",\"name\":\"$name\",\"content\":\"$target\",\"proxied\":true}")
            echo "$UPDATE_RESPONSE" | jq .
        else
            log "INFO" "CNAME record for $name is already up to date."
        fi
    }

    # Create/update DNS records for all domains
#     check_and_update_record "$DOMAIN"
    check_and_update_record "$FULL_SUBDOMAIN"
}


set_permissions() {
    log "INFO" "Creating required application directories..."

    # Use environment variables instead of hardcoded paths
    BASE_DIR="${APP_BASE_DIR}"
    DEPLOY_USER="${DEPLOY_USER}"

    log "INFO" "Using base directory: $BASE_DIR for user: $DEPLOY_USER"

    # Create directories with sudo if needed, then fix ownership
    log "INFO" "Creating directory structure (may require sudo)..."
    sudo mkdir -p "$BASE_DIR/logs"
    sudo mkdir -p "$BASE_DIR/data"
    sudo mkdir -p "$BASE_DIR/backups"
    sudo mkdir -p "$BASE_DIR/dataprotection-keys"
    sudo mkdir -p "$BASE_DIR/wwwroot/images"
    sudo mkdir -p "$BASE_DIR/wwwroot/uploads"

    # Set ownership using environment variable
    log "INFO" "Setting AiCV application directory ownership to $DEPLOY_USER:$DEPLOY_USER..."
    sudo chown -R "$DEPLOY_USER:$DEPLOY_USER" "$BASE_DIR"

    # Set proper permissions for backups directory to allow both containers access
    log "INFO" "Setting special permissions for backups directory..."
    sudo chmod 777 "$BASE_DIR/backups"  # Allow both containers to write
    sudo chown "$DEPLOY_USER:$DEPLOY_USER" "$BASE_DIR/backups"

    # Now set proper permissions (this will work after ownership is fixed)
    log "INFO" "Setting AiCV application directory permissions..."
    chmod 755 "$BASE_DIR"
    chmod 755 "$BASE_DIR/logs"
    chmod 755 "$BASE_DIR/data"
    chmod 700 "$BASE_DIR/dataprotection-keys"  # More restrictive for security keys
    chmod 755 "$BASE_DIR/wwwroot"
    chmod 755 "$BASE_DIR/wwwroot/images"
    chmod 755 "$BASE_DIR/wwwroot/uploads"

    log "INFO" "Setting observability stack directory permissions..."


    log "INFO" "All directories created with proper permissions!"
    log "INFO" "AiCV app directories: $BASE_DIR"
    log "INFO" "Owner: $DEPLOY_USER:$DEPLOY_USER"
    log "INFO" "DataProtection keys: $BASE_DIR/dataprotection-keys (700)"
    log "INFO" "Backups directory: $BASE_DIR/backups (777 - accessible by both app and DB containers)"
    log "INFO" "Image directories ready for uploads"
}

fix_backup_permissions() {
    log "INFO" "Fixing backup directory permissions for SQL Server access..."

    BACKUPS_HOST_DIR="${HOST_BACKUPS_DIR}"
    DEPLOY_USER="${DEPLOY_USER}"

    # Ensure the backup directory has the right permissions for both containers
    if [ -d "$BACKUPS_HOST_DIR" ]; then
        sudo chmod 777 "$BACKUPS_HOST_DIR"
        sudo chown "$DEPLOY_USER:$DEPLOY_USER" "$BACKUPS_HOST_DIR"
        log "INFO" "Backup directory permissions fixed: $BACKUPS_HOST_DIR (777) owned by $DEPLOY_USER:$DEPLOY_USER"

        # Also fix any existing backup files
        find "$BACKUPS_HOST_DIR" -name "*.bak" -exec sudo chmod 666 {} \; 2>/dev/null || true
        log "INFO" "Existing backup files permissions updated"
    else
        log "WARN" "Backup directory not found: $BACKUPS_HOST_DIR"
    fi
}

initialize_static_images() {
    log "INFO" "Initializing static images..."

    CONTAINER_NAME="${APP_CONTAINER_NAME:-aicv-app}"
    STATIC_IMAGES_HOST_DIR="${HOST_STATIC_IMAGES_DIR}"
    DEPLOY_USER="${DEPLOY_USER}"

    # Wait for container to be ready
    log "INFO" "Waiting for container $CONTAINER_NAME to be ready..."
    for i in {1..30}; do
        if docker ps | grep -q "$CONTAINER_NAME"; then
            log "INFO" "Container $CONTAINER_NAME is running"
            break
        fi
        if [ $i -eq 30 ]; then
            log "WARN" "Container $CONTAINER_NAME not found after 30 seconds, skipping static images initialization"
            return
        fi
        sleep 1
    done

    # Check if static images are already initialized
    if [ -f "$STATIC_IMAGES_HOST_DIR/.static_images_initialized" ]; then
        log "INFO" "Static images already initialized, skipping..."
        return
    fi

    # Check if container has static images backup
    if docker exec "$CONTAINER_NAME" test -d "/app/wwwroot/images_static_backup" 2>/dev/null; then
        log "INFO" "Found static images backup in container, copying to persistent storage..."

        # Create temporary directory for extraction
        TEMP_DIR="/tmp/aicv_static_images_$$"
        mkdir -p "$TEMP_DIR"

        # Copy static images from container to temporary location
        docker cp "$CONTAINER_NAME:/app/wwwroot/images_static_backup/." "$TEMP_DIR/" || {
            log "WARN" "Failed to copy static images from container"
            rm -rf "$TEMP_DIR"
            return
        }

        # Copy from temporary location to persistent storage
        cp -r "$TEMP_DIR/"* "$STATIC_IMAGES_HOST_DIR/" 2>/dev/null || log "WARN" "Some static images may not have copied correctly"

        # Create initialization marker
        touch "$STATIC_IMAGES_HOST_DIR/.static_images_initialized"

        # Set proper permissions using environment variable
        sudo chown -R "$DEPLOY_USER:$DEPLOY_USER" "$STATIC_IMAGES_HOST_DIR"
        chmod 755 "$STATIC_IMAGES_HOST_DIR"
        find "$STATIC_IMAGES_HOST_DIR" -type f -exec chmod 644 {} \; 2>/dev/null || true
        find "$STATIC_IMAGES_HOST_DIR" -type d -exec chmod 755 {} \; 2>/dev/null || true

        # Cleanup
        rm -rf "$TEMP_DIR"

        log "INFO" "Static images copied successfully!"

    else
        log "INFO" "No static images backup found in container, creating initialization marker"
        touch "$STATIC_IMAGES_HOST_DIR/.static_images_initialized"
        sudo chown "$DEPLOY_USER:$DEPLOY_USER" "$STATIC_IMAGES_HOST_DIR/.static_images_initialized"
    fi
}

start_services() {
    log "INFO" "Cleaning up previous containers and networks..."

    # Stop and remove all containers, including those not in docker-compose
    log "INFO" "Stopping all running containers..."
    docker stop $(docker ps -aq --filter "name=aicv-") 2>/dev/null || true
    docker rm $(docker ps -aq --filter "name=aicv-") 2>/dev/null || true

    # Remove all unused networks
    log "INFO" "Cleaning up networks..."
    # docker network prune -f

    # Remove any dangling containers
    log "INFO" "Removing dangling containers..."
    # docker container prune -f

    # Check if ports are in use and kill processes if needed
    log "INFO" "Checking for processes using required ports..."
    for port in ${PORT_CLEANUP_LIST}; do
        if lsof -i :$port >/dev/null 2>&1; then
            log "WARN" "Port $port is in use. Attempting to free it..."
            sudo fuser -k $port/tcp 2>/dev/null || true
            sleep 2
        fi
    done

    # Select compose file based on DB_PROVIDER
    COMPOSE_FILE=$(get_compose_file)
    log "INFO" "Using compose file: $COMPOSE_FILE (DB_PROVIDER=${DB_PROVIDER:-SqlServer})"

    log "INFO" "Pulling latest images..."
    docker-compose -f "$COMPOSE_FILE" pull

    log "INFO" "Starting containers..."
    docker-compose -f "$COMPOSE_FILE" up -d --remove-orphans

    # Fix backup permissions and initialize static images after containers are up
    fix_backup_permissions
    initialize_static_images

    log "INFO" "Deployment complete. Visit https://$DOMAIN"
    log "INFO" "Deployment complete. Visit https://$FULL_SUBDOMAIN"
}


ensure_shared_sql() {
    # Use environment variables from .env (loaded at script start)
    SA_PASSWORD="${DB_PASSWORD}"
    DB_NAME="${DB_NAME:-aicv_db}"
    
    if ! docker ps --format '{{.Names}}' | grep -q '^shared-sqlserver$'; then
        log "INFO" "Starting shared SQL Server..."
        SHARED_SQL_DIR="${SHARED_SQL_DIR:-/home/shared-sql}"
        mkdir -p "$SHARED_SQL_DIR"
        cd "$SHARED_SQL_DIR"
        docker-compose up -d
        
        log "INFO" "Waiting for SQL Server to be ready..."
        for i in {1..30}; do
            if docker exec shared-sqlserver /opt/mssql-tools18/bin/sqlcmd \
                -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" &>/dev/null; then
                log "INFO" "SQL Server is ready!"
                break
            fi
            sleep 2
        done
        cd - > /dev/null
    fi
    
    # Initialize database if init script exists (optional - for shared SQL Server setups)
    SHARED_SQL_DIR="${SHARED_SQL_DIR:-/home/shared-sql}"
    if [ -f "$SHARED_SQL_DIR/init-db.sh" ]; then
        log "INFO" "Ensuring database '$DB_NAME' exists in shared SQL Server..."
        "$SHARED_SQL_DIR/init-db.sh" "$DB_NAME" "$SA_PASSWORD"
    else
        log "INFO" "No init-db.sh found at $SHARED_SQL_DIR - skipping database initialization (assuming DB exists or will be auto-created)"
    fi
}

main() {
    log "INFO" "Starting deployment process..."
    log "INFO" "Database Provider: ${DB_PROVIDER:-SqlServer}"
    remove_docker_credsstore
    fix_env_file
    install_dependencies
    ensure_docker_permissions
    set_permissions
    
    # Start appropriate database based on DB_PROVIDER
    # PostgreSQL is included in docker-compose.postgres.yml, SQL Server uses shared-sql
    if [ "${DB_PROVIDER:-SqlServer}" != "PostgreSQL" ]; then
        ensure_shared_sql
    fi
    
    ensure_dns_record
    start_services
}

main