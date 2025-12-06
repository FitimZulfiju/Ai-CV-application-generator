window.cvScaler = {
    fitContentToPages: function () {
        const pages = document.querySelectorAll('.page');
        // 1123px is full A4 @ 96dpi. Browser margins (header/footer/printable area) reduce this significantly.
        // With 1cm margins (approx 38px each), available height is ~1047px.
        // We set safety limit to 1010px to prevent footer spilling.
        const maxHeight = 1010; 
        const minFontSizeRem = 0.55; 
        const maxFontSizeRem = 1.15; 
        const stepRem = 0.02;

        pages.forEach(page => {
            let fontSizeRem = 0.95; // Start with larger standard size to encourage filling
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
