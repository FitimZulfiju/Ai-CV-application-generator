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
                const newVersionTag = data.newVersionTag;
                const isUpdateScheduled = data.isUpdateScheduled;
                const scheduledUpdateTime = data.scheduledUpdateTime;

                if (currentVersion === null) {
                    currentVersion = newVersion;
                }

                // Case 1: Update already applied (version mismatch)
                if (currentVersion !== newVersion) {
                    console.log(`Version changed from ${currentVersion} to ${newVersion}. Triggering reload notification...`);
                    showUpdateNotification('applied', newVersion);
                }
                // Case 2: Update is already scheduled on server - show countdown
                else if (isUpdateScheduled && scheduledUpdateTime) {
                    console.log(`Update already scheduled for ${scheduledUpdateTime}. Showing countdown...`);
                    showUpdateNotification('pending', newVersionTag, new Date(scheduledUpdateTime));
                }
                // Case 3: Update pending in registry but not yet scheduled - schedule it
                else if (isUpdateAvailable) {
                    console.log(`New version found in registry. Scheduling update on server...`);
                    await scheduleUpdateOnServer();
                    showUpdateNotification('pending', newVersionTag, null);
                }
            }
        } catch (error) {
            console.warn('Failed to check version:', error);
        }
    }

    async function scheduleUpdateOnServer() {
        try {
            const response = await fetch('/api/schedule-update', { method: 'POST' });
            if (response.ok) {
                const data = await response.json();
                console.log(`Update scheduled on server for: ${data.scheduledUpdateTime}`);
                return data.scheduledUpdateTime;
            }
        } catch (error) {
            console.warn('Failed to schedule update on server:', error);
        }
        return null;
    }

    function showUpdateNotification(type, version = '', serverScheduledTime = null) {
        notificationShown = true;
        const isPending = type === 'pending';

        // 1. Completely Suppress Blazor UI
        // We do this aggressively because we are taking over the UX
        const suppressBlazorStyle = document.createElement('style');
        suppressBlazorStyle.innerHTML = `
            #components-reconnect-modal, 
            .components-reconnect-modal,
            .components-reconnect-container { 
                display: none !important; 
                visibility: hidden !important; 
                opacity: 0 !important; 
                pointer-events: none !important; 
            }
        `;
        document.head.appendChild(suppressBlazorStyle);

        // 2. Create Banner (MudBlazor "Filled Warning" Style)
        const banner = document.createElement('div');
        banner.style.boxShadow = '0px 2px 4px -1px rgba(0,0,0,0.2), 0px 4px 5px 0px rgba(0,0,0,0.14), 0px 1px 10px 0px rgba(0,0,0,0.12)';
        banner.style.position = 'fixed';
        banner.style.top = '0';
        banner.style.left = '0';
        banner.style.width = '100%';
        banner.style.zIndex = '2147483647'; // Max z-index
        banner.style.backgroundColor = '#ff9800';
        banner.style.color = '#ffffff';
        banner.style.display = 'flex';
        banner.style.alignItems = 'center';
        banner.style.padding = '12px 24px';
        banner.style.fontFamily = 'Roboto, Helvetica, Arial, sans-serif';
        banner.style.fontSize = '1rem';
        banner.style.lineHeight = '1.5';
        banner.id = 'update-notification-banner';

        // Icon
        const iconDiv = document.createElement('div');
        iconDiv.style.marginRight = '22px';
        iconDiv.style.display = 'flex';
        iconDiv.style.alignItems = 'center';
        iconDiv.innerHTML = `<svg style="width: 24px; height: 24px; fill: white;" viewBox="0 0 24 24"><path d="M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"></path></svg>`;
        banner.appendChild(iconDiv);

        // Message
        const messageDiv = document.createElement('div');
        messageDiv.style.flex = '1';

        const titleText = document.createElement('div');
        const versionString = version ? ` (${version})` : '';
        titleText.innerText = isPending ? `Update Available${versionString}` : `New version applied${versionString}`;
        titleText.style.fontWeight = '500';
        messageDiv.appendChild(titleText);

        const subText = document.createElement('div');
        subText.innerText = isPending
            ? 'We are improving the system. Your work is saved locally. Brief restart in 5 minutes.'
            : 'The application has been updated. Please reload to load the new version.';
        subText.style.fontSize = '0.875rem';
        subText.style.opacity = '0.9';
        messageDiv.appendChild(subText);

        banner.appendChild(messageDiv);

        // Action / Countdown
        const actionDiv = document.createElement('div');
        actionDiv.style.marginLeft = 'auto';

        const countdownSpan = document.createElement('span');
        countdownSpan.style.fontWeight = 'bold';
        actionDiv.appendChild(countdownSpan);
        banner.appendChild(actionDiv);
        document.body.appendChild(banner);

        function formatTime(s) {
            const m = Math.floor(s / 60);
            const sec = s % 60;
            return `${m}:${sec.toString().padStart(2, '0')}`;
        }

        async function waitForServerAndReload() {
            // Update UI to show we are waiting
            countdownSpan.innerText = "Installing updates...";
            subText.innerText = "The server is restarting. We will reload you automatically when it's back...";

            // Poll every 2 seconds
            const pollInterval = setInterval(async () => {
                try {
                    const response = await fetch('/api/version', { cache: 'no-store' });
                    if (response.ok) {
                        const data = await response.json();
                        // If we get a valid version back, the server is up!
                        if (data.version) {
                            console.log('Server is back! Reloading...');
                            clearInterval(pollInterval);
                            location.reload();
                        }
                    }
                } catch (e) {
                    console.log('Waiting for server...');
                }
            }, 2000);
        }

        // Calculate seconds left based on server's scheduled time
        function getSecondsLeft() {
            if (serverScheduledTime) {
                const now = new Date();
                const diff = (serverScheduledTime.getTime() - now.getTime()) / 1000;
                return Math.max(0, Math.floor(diff));
            }
            return 300; // Default 5 minutes if no server time
        }

        // Start Countdown if pending
        if (isPending) {
            let secondsLeft = getSecondsLeft();
            countdownSpan.innerText = `Updating in ${formatTime(secondsLeft)}`;

            reloadTimer = setInterval(() => {
                secondsLeft = getSecondsLeft();
                if (secondsLeft <= 0) {
                    clearInterval(reloadTimer);
                    // Server is triggering the update - wait for it to come back
                    waitForServerAndReload();
                } else {
                    countdownSpan.innerText = `Updating in ${formatTime(secondsLeft)}`;
                }
            }, 1000);
        } else {
            countdownSpan.innerText = 'Ready to reload';
            // If already applied, just wait a moment then reload or let user click? 
            // Logic in original code was just showing banner for applied.
            // We can let them reload manually or auto-reload after a short delay if preferred.
            // For now, let's auto-reload after 10s for "applied" case to be helpful?
            // Or stick to manual to be safe. Original code implied manual or simple message.
        }
    }

    checkVersion();
    setInterval(checkVersion, 10000);
}

startAutoRefresh();

