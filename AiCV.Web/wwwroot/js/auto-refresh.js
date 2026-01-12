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
                const secondsRemaining = data.secondsRemaining;

                if (currentVersion === null) {
                    currentVersion = newVersion;
                }

                // Case 1: Update already applied (version mismatch)
                if (currentVersion !== newVersion) {
                    console.log(`Version changed from ${currentVersion} to ${newVersion}. Triggering reload notification...`);
                    showUpdateNotification('applied', newVersion);
                }
                // Case 2: Update is already scheduled on server - show countdown
                else if (isUpdateScheduled) {
                    console.log(`Update already scheduled. Remaining: ${secondsRemaining}s. Showing countdown...`);
                    showUpdateNotification('pending', newVersionTag, null, secondsRemaining);
                }
                // Case 3: Update pending in registry but not yet scheduled - schedule it
                else if (isUpdateAvailable) {
                    console.log(`New version found in registry. Scheduling update on server...`);
                    const result = await scheduleUpdateOnServer();
                    showUpdateNotification('pending', newVersionTag, null, result?.secondsRemaining);
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

                // Ensure we parse the time correctly
                if (data.scheduledUpdateTime) {
                    return new Date(data.scheduledUpdateTime);
                }
            }
        } catch (error) {
            console.warn('Failed to schedule update on server:', error);
        }
        return null;
    }

    function showUpdateNotification(type, version = '', serverScheduledTime = null) {
        notificationShown = true;
        const isPending = type === 'pending';

        // If no server time provided, fallback to 5 minutes from now for local display
        let localCountdownEnd = null;
        if (isPending && !serverScheduledTime) {
            localCountdownEnd = new Date();
            localCountdownEnd.setMinutes(localCountdownEnd.getMinutes() + 3);
        }

        // 1. (Validation) We removed the suppression of Blazor UI to verify if the app is disconnected
        // If the server restarts early, the user should see 'Attempting to reconnect...'

        // 2. Create Banner (MudBlazor "Filled Warning" Style)

        // 2. Create Banner (MudBlazor "Filled Warning" Style)
        const banner = document.createElement('div');
        banner.style.boxShadow = '0px -2px 10px rgba(0,0,0,0.3)';
        banner.style.position = 'fixed';
        banner.style.bottom = '0'; // Moved to bottom
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
        iconDiv.style.marginRight = '20px';
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
            ? 'We are improving the system. Your work is saved locally. Brief restart in 3 minutes.'
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

            // Poll every 2.5 seconds
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
            }, 2500);
        }

        // Calculate seconds left based on targetTime
        function getSecondsLeft() {
            if (targetTime) {
                const now = new Date();
                const diff = (targetTime.getTime() - now.getTime()) / 1000;
                return Math.max(0, Math.floor(diff));
            }
            return 0;
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
            // Auto-reload after versions change if type is applied
            setTimeout(() => location.reload(), 5000);
        }
    }

    checkVersion();
    setInterval(checkVersion, 15000);
}

startAutoRefresh();
