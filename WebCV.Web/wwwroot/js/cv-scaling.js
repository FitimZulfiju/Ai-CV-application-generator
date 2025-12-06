window.cvScaler = {
    fitContentToPages: function () {
        const pages = document.querySelectorAll('.page');
        const stepRem = 0.01;

        pages.forEach(page => {
            // Check if this page belongs to a Cover Letter
            const isCoverLetter = page.closest('.cover-letter') !== null;
            const hasFooter = page.querySelector('.footer');

            let maxHeight, minFontSizeRem;

            if (isCoverLetter) {
                // COVER LETTER STRATEGY:
                // Start with Standard Size (1.0rem). 
                // Only shrink if it overflows 900px (safety buffer). Never grow beyond standard.
                maxHeight = 900;
                minFontSizeRem = 0.25; 
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
            
            // SHRINK logic (Applies to ALL)
            if (currentHeight > maxHeight) {
                while (currentHeight > maxHeight && fontSizeRem > minFontSizeRem) {
                    fontSizeRem -= stepRem;
                    page.style.fontSize = fontSizeRem + 'rem';
                    currentHeight = page.scrollHeight;
                }
            } 
 
            // GROW logic (Applies to CV ONLY - we want CV pages to look full)
            else if (!isCoverLetter) { 
                // Don't let CV font get comically large, cap at 1.35rem
                const maxCvFont = 1.35; 
                
                while (currentHeight < maxHeight && fontSizeRem < maxCvFont) {
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
