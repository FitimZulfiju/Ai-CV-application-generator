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

                if (currentVersion === null) {
                    currentVersion = newVersion;
                } else if (currentVersion !== newVersion) {
                    console.log(`Version changed from ${currentVersion} to ${newVersion}. Triggering notification...`);
                    showUpdateNotification(newVersion);
                }
            }
        } catch (error) {
            console.warn('Failed to check version:', error);
        }
    }

    function showUpdateNotification(newVersion) {
        notificationShown = true;

        // 1. Suppress Blazor Overlay
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
        titleText.innerText = `New update available (${newVersion})`;
        titleText.style.fontWeight = '500';
        messageDiv.appendChild(titleText);

        const subText = document.createElement('div');
        subText.innerText = `Connection has been reset. Please copy your work before reloading.`;
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
        let secondsLeft = 60;
        countdownSpan.innerText = `Auto-reload in ${secondsLeft}s`;
        countdownSpan.style.fontSize = '0.875rem';
        countdownSpan.style.marginRight = '16px';
        actionDiv.appendChild(countdownSpan);

        // Reload Button (MudButton Look-alike: Filled, White Text on Darker Orange)
        const reloadBtn = document.createElement('button');
        reloadBtn.innerText = 'RELOAD NOW';
        reloadBtn.style.backgroundColor = 'rgba(255, 255, 255, 0.15)'; // Semi-transparent white overlay
        reloadBtn.style.color = '#ffffff';
        reloadBtn.style.border = 'none';
        reloadBtn.style.borderRadius = '4px';
        reloadBtn.style.padding = '6px 16px';
        reloadBtn.style.fontFamily = 'inherit';
        reloadBtn.style.fontWeight = '500';
        reloadBtn.style.fontSize = '0.875rem';
        reloadBtn.style.textTransform = 'uppercase';
        reloadBtn.style.letterSpacing = '0.02857em';
        reloadBtn.style.cursor = 'pointer';
        reloadBtn.style.transition = 'background-color 250ms cubic-bezier(0.4, 0, 0.2, 1)';

        // Hover effect
        reloadBtn.onmouseover = () => reloadBtn.style.backgroundColor = 'rgba(255, 255, 255, 0.25)';
        reloadBtn.onmouseout = () => reloadBtn.style.backgroundColor = 'rgba(255, 255, 255, 0.15)';
        reloadBtn.onclick = () => location.reload();

        actionDiv.appendChild(reloadBtn);
        banner.appendChild(actionDiv);

        document.body.appendChild(banner);

        // 3. Start Countdown
        reloadTimer = setInterval(() => {
            secondsLeft--;
            countdownSpan.innerText = `Auto-reload in ${secondsLeft}s`;
            if (secondsLeft <= 0) {
                clearInterval(reloadTimer);
                location.reload();
            }
        }, 1000);
    }

    checkVersion();
    setInterval(checkVersion, 10000);
}

startAutoRefresh();
