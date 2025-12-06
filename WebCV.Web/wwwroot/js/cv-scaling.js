window.cvScaler = {
    fitContentToPages: function () {
        const pages = document.querySelectorAll('.page');
        const stepRem = 0.01;

        pages.forEach(page => {
            // Check if this page belongs to a Cover Letter
            const isCoverLetter = page.closest('.cover-letter') !== null;
            const hasFooter = page.querySelector('.footer');

            let maxHeight, maxFontSizeRem, minFontSizeRem;

            if (isCoverLetter) {
                // COVER LETTER STRATEGY:
                // Start with Standard Size (1.0rem). 
                // Only shrink if it overflows 900px (safety buffer). Never grow beyond standard.
                maxHeight = 900;
                minFontSizeRem = 0.45; 
            } else {
                // CV STRATEGY (Smart Scaling):
                // Start with Standard Size (0.95rem).
                // Only shrink if it overflows the specific page limit.
                maxHeight = hasFooter ? 990 : 1035; 
                minFontSizeRem = 0.55;
            }

            // STANDARD STARTING SIZE
            // We trust the standard size looks best.
            let fontSizeRem = isCoverLetter ? 1.0 : 0.95; 

            page.style.fontSize = fontSizeRem + 'rem';
            
            let currentHeight = page.scrollHeight;
            
            // SHRINK ONLY logic
            if (currentHeight > maxHeight) {
                while (currentHeight > maxHeight && fontSizeRem > minFontSizeRem) {
                    fontSizeRem -= stepRem;
                    page.style.fontSize = fontSizeRem + 'rem';
                    currentHeight = page.scrollHeight;
                }
            }
            // Grow logic REMOVED. We accept empty space for better consistency.
        });
    }
};
