#!/usr/bin/env bash
set -o errexit
set -o pipefail

log() { level="$1"; shift; printf "[%s] %s\n" "$level" "$*"; }

# Load .env file
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/.env" ]; then
    set -o allexport
    source "$SCRIPT_DIR/.env"
    set +o allexport
fi

# Configuration
TUNNEL_NAME="${TUNNEL_NAME:-aicv-tunnel}"
# Construct HOSTNAME if not set, using SUBDOMAIN and DOMAIN
HOSTNAME="${FULL_SUBDOMAIN:-${SUBDOMAIN}.${DOMAIN}}"
SERVICE="${APP_SERVICE_URL:-http://aicv-app:8080}"
SHARED_CLOUDFLARED_DIR="${SHARED_CLOUDFLARED_DIR:-$HOME/.cloudflared}"
MOUNT_DIR="${CLOUDFLARED_MOUNT_DIR:-$SCRIPT_DIR/cloudflared-mount}"
ENV_FILE="${ENV_FILE_PATH:-$SCRIPT_DIR/.env}"

# Ensure cloudflared is logged in
if [ ! -f "$SHARED_CLOUDFLARED_DIR/cert.pem" ]; then
    log "ERROR" "Cloudflare cert not found. Run 'cloudflared tunnel login' first."
    exit 1
fi

# Check if tunnel exists, create if not
TUNNEL_ID=$(cloudflared tunnel list --output json 2>/dev/null | python3 -c "import sys,json; tunnels=json.load(sys.stdin); print(next((t['id'] for t in tunnels if t['name']=='$TUNNEL_NAME'), ''))" 2>/dev/null || echo "")

if [ -z "$TUNNEL_ID" ]; then
    log "INFO" "Creating new tunnel: $TUNNEL_NAME"
    OUTPUT=$(cloudflared tunnel create "$TUNNEL_NAME" 2>&1)
    TUNNEL_ID=$(echo "$OUTPUT" | grep -oP 'id \K[a-f0-9-]+' | head -1)
    if [ -z "$TUNNEL_ID" ]; then
        log "ERROR" "Failed to create tunnel"
        echo "$OUTPUT"
        exit 1
    fi
    log "SUCCESS" "Created tunnel with ID: $TUNNEL_ID"
    cp "/root/.cloudflared/$TUNNEL_ID.json" "$SHARED_CLOUDFLARED_DIR/$TUNNEL_ID.json" 2>/dev/null || true
else
    log "INFO" "Using existing tunnel: $TUNNEL_NAME ($TUNNEL_ID)"
fi

# Update .env with TUNNEL_ID
if grep -q "^TUNNEL_ID=" "$ENV_FILE" 2>/dev/null; then
    sed -i "s/^TUNNEL_ID=.*/TUNNEL_ID=$TUNNEL_ID/" "$ENV_FILE"
else
    echo "TUNNEL_ID=$TUNNEL_ID" >> "$ENV_FILE"
fi
log "SUCCESS" "Updated TUNNEL_ID in $ENV_FILE"

# Create DNS route
log "INFO" "Creating DNS route for $HOSTNAME..."
cloudflared tunnel route dns "$TUNNEL_ID" "$HOSTNAME" 2>/dev/null || log "WARN" "DNS route may already exist"

# Setup mount directory and config
mkdir -p "$MOUNT_DIR"
CRED_FILE="$SHARED_CLOUDFLARED_DIR/$TUNNEL_ID.json"
if [ ! -f "$CRED_FILE" ]; then
    log "ERROR" "Credentials not found at $CRED_FILE"
    exit 1
fi
cp "$CRED_FILE" "$MOUNT_DIR/$TUNNEL_ID.json"

cat > "$MOUNT_DIR/config.yml" << EOF
tunnel: $TUNNEL_ID
credentials-file: /home/nonroot/.cloudflared/$TUNNEL_ID.json

ingress:
  - hostname: $HOSTNAME
    service: $SERVICE
  - service: http_status:404
EOF

chmod 644 "$MOUNT_DIR/config.yml" "$MOUNT_DIR/$TUNNEL_ID.json"
log "SUCCESS" "Tunnel setup complete for AiCV"
log "INFO" "Config: $MOUNT_DIR/config.yml"