window.aiCvLayoutViewport = (() => {
    let resizeHandler = null;
    let lastIsMobile = null;

    function getIsMobile(breakpoint) {
        return window.innerWidth < breakpoint;
    }

    return {
        register(dotNetReference, breakpoint) {
            const notify = () => {
                const isMobile = getIsMobile(breakpoint);
                if (lastIsMobile === isMobile) {
                    return;
                }

                lastIsMobile = isMobile;
                dotNetReference.invokeMethodAsync("OnViewportChanged", isMobile);
            };

            if (resizeHandler) {
                window.removeEventListener("resize", resizeHandler);
            }

            resizeHandler = notify;
            window.addEventListener("resize", resizeHandler, { passive: true });
            notify();
        }
    };
})();
