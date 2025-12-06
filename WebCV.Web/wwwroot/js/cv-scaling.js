window.cvScaler = {
    fitContentToPages: function () {
        const pages = document.querySelectorAll('.page');
        const stepRem = 0.01;

        pages.forEach(page => {
            // Dynamic "content-aware" logic:
            // Check if the page contains a footer
            const hasFooter = page.querySelector('.footer');

            // 1035px for standard body pages (filling space aggressively).
            // 990px for the footer page (strict safety to keep footer visible).
            const maxHeight = hasFooter ? 990 : 1035; 
            
            // We allow ALL pages to potentially grow to 1.35rem if they are empty,
            // relying on the maxHeight check to stop them if they get too full.
            const maxFontSizeRem = 1.35;
            const minFontSizeRem = 0.55;

            let fontSizeRem = 0.95; 
            page.style.fontSize = fontSizeRem + 'rem';
            
            let currentHeight = page.scrollHeight;
            
            if (currentHeight > maxHeight) {
                // Shrink
                while (currentHeight > maxHeight && fontSizeRem > minFontSizeRem) {
                    fontSizeRem -= stepRem;
                    page.style.fontSize = fontSizeRem + 'rem';
                    currentHeight = page.scrollHeight;
                }
            } else {
                // Grow logic
                while (currentHeight < maxHeight && fontSizeRem < maxFontSizeRem) {
                    let nextFontSizeRem = fontSizeRem + stepRem;
                    page.style.fontSize = nextFontSizeRem + 'rem';
                    
                    if (page.scrollHeight > maxHeight) {
                        // Revert one step and stop
                        page.style.fontSize = fontSizeRem + 'rem';
                        break;
                    }
                    fontSizeRem = nextFontSizeRem;
                    currentHeight = page.scrollHeight;
                }
            }
        });
    }
};
