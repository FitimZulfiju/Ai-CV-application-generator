export function startAutoRefresh() {
    let currentVersion = null;
    let reloadTimer = null;
    let notificationShown = false;

    async function checkVersion() {
        if (notificationShown) return;

        try {
            const response = await fetch('/api/version');
            if (response.ok) {
                const data = await response.json();
                const newVersion = data.version;
                const isUpdateAvailable = data.isUpdateAvailable;

                if (currentVersion === null) {
                    currentVersion = newVersion;
                }

                // Case 1: Update already applied (version mismatch)
                if (currentVersion !== newVersion) {
                    console.log(`Version changed from ${currentVersion} to ${newVersion}. Triggering reload notification...`);
                    showUpdateNotification('applied', newVersion);
                }
                // Case 2: Update pending in registry
                else if (isUpdateAvailable) {
                    console.log(`New version found in registry. Triggering pending update notification...`);
                    showUpdateNotification('pending');
                }
            }
        } catch (error) {
            console.warn('Failed to check version:', error);
        }
    }

    function showUpdateNotification(type, version = '') {
        notificationShown = true;
        const isPending = type === 'pending';

        // 1. Suppress Blazor Overlay (only if update is already applied and connection is likely broken)
        if (!isPending) {
            setInterval(() => {
                const reconnectModal = document.querySelector('#components-reconnect-modal');
                if (reconnectModal) {
                    reconnectModal.style.display = 'none';
                    reconnectModal.style.visibility = 'hidden';
                    reconnectModal.style.opacity = '0';
                    reconnectModal.style.pointerEvents = 'none';
                }
                const reconnectContainer = document.querySelector('.components-reconnect-container');
                if (reconnectContainer) {
                    reconnectContainer.style.display = 'none';
                }
            }, 500);
        }

        // 2. Create Banner (MudBlazor "Filled Warning" Look-alike)
        const banner = document.createElement('div');

        // Material Design Elevation 4
        banner.style.boxShadow = '0px 2px 4px -1px rgba(0,0,0,0.2), 0px 4px 5px 0px rgba(0,0,0,0.14), 0px 1px 10px 0px rgba(0,0,0,0.12)';
        banner.style.position = 'fixed';
        banner.style.top = '0';
        banner.style.left = '0';
        banner.style.width = '100%';
        banner.style.zIndex = '2000000000'; // Max z-index
        banner.style.backgroundColor = '#ff9800'; // MudBlazor Warning Color
        banner.style.color = '#ffffff';
        banner.style.display = 'flex';
        banner.style.alignItems = 'center';
        banner.style.padding = '12px 24px';
        banner.style.fontFamily = '"Roboto", "Helvetica", "Arial", sans-serif';
        banner.style.fontSize = '1rem';
        banner.style.fontWeight = '400';
        banner.style.letterSpacing = '0.00938em';
        banner.style.lineHeight = '1.5';
        banner.id = 'update-notification-banner';

        // Icon Container
        const iconDiv = document.createElement('div');
        iconDiv.style.marginRight = '22px';
        iconDiv.style.display = 'flex';
        iconDiv.style.alignItems = 'center';

        // SVG Icon (Warning Triangle)
        iconDiv.innerHTML = `<svg class="mud-icon-root mud-svg-icon mud-inherit-text mud-icon-size-medium" focusable="false" viewBox="0 0 24 24" aria-hidden="true" style="width: 24px; height: 24px; fill: white;"><path d="M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"></path></svg>`;
        banner.appendChild(iconDiv);

        // Message Container
        const messageDiv = document.createElement('div');
        messageDiv.style.flex = '1';
        messageDiv.style.padding = '8px 0';

        const titleText = document.createElement('div');
        titleText.innerText = isPending ? 'Planned maintenance: A new version is ready' : `New version applied (${version})`;
        titleText.style.fontWeight = '500';
        messageDiv.appendChild(titleText);

        const subText = document.createElement('div');
        subText.innerText = isPending
            ? 'A new version is available. Your work is safely stored locally. Restarting in 5 minutes.'
            : 'The application has been updated. Please reload to see the changes.';
        subText.style.fontSize = '0.875rem';
        subText.style.opacity = '0.9';
        messageDiv.appendChild(subText);

        banner.appendChild(messageDiv);

        // Action Container
        const actionDiv = document.createElement('div');
        actionDiv.style.marginLeft = 'auto';
        actionDiv.style.display = 'flex';
        actionDiv.style.alignItems = 'center';
        actionDiv.style.gap = '16px';

        // Countdown
        const countdownSpan = document.createElement('span');
        let secondsLeft = 300;

        function formatTime(seconds) {
            const m = Math.floor(seconds / 60);
            const s = seconds % 60;
            return `${m}:${s.toString().padStart(2, '0')}`;
        }

        countdownSpan.innerText = isPending ? `Restart in ${formatTime(secondsLeft)}` : `Reload in ${formatTime(secondsLeft)}`;
        countdownSpan.style.fontSize = '0.875rem';
        countdownSpan.style.marginRight = '16px';
        actionDiv.appendChild(countdownSpan);

        async function triggerUpdate() {
            if (isPending) {
                try {
                    // Trigger Watchtower update via backend
                    const response = await fetch('/api/trigger-update', { method: 'POST' });
                    if (response.ok) {
                        console.log('Update triggered successfully.');
                    }
                } catch (error) {
                    console.error('Failed to trigger update:', error);
                    location.reload(); // Fallback
                }
            } else {
                location.reload();
            }
        }

        banner.appendChild(actionDiv);
        document.body.appendChild(banner);

        // 3. Start Countdown
        reloadTimer = setInterval(() => {
            secondsLeft--;
            countdownSpan.innerText = isPending ? `Restart in ${formatTime(secondsLeft)}` : `Reload in ${formatTime(secondsLeft)}`;
            if (secondsLeft <= 0) {
                clearInterval(reloadTimer);
                triggerUpdate();
            }
        }, 1000);
    }

    checkVersion();
    setInterval(checkVersion, 10000);
}

startAutoRefresh();
