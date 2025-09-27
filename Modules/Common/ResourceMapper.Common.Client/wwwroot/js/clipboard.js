window.getClipboardData = async () => {
    try {
        const clipboardData = {
            text: '',
            html: ''
        };

        // Try to get text content
        if (navigator.clipboard && navigator.clipboard.readText) {
            try {
                clipboardData.text = await navigator.clipboard.readText();
            } catch (e) {
                console.warn('Failed to read clipboard text:', e);
            }
        }

        // Try to get HTML content using the newer Clipboard API
        if (navigator.clipboard && navigator.clipboard.read) {
            try {
                const clipboardItems = await navigator.clipboard.read();
                for (const item of clipboardItems) {
                    if (item.types.includes('text/html')) {
                        const htmlBlob = await item.getType('text/html');
                        clipboardData.html = await htmlBlob.text();
                        break;
                    }
                }
            } catch (e) {
                console.warn('Failed to read clipboard HTML:', e);
            }
        }

        // Fallback: Try to use the legacy execCommand method for HTML
        if (!clipboardData.html && document.queryCommandSupported && document.queryCommandSupported('paste')) {
            try {
                // Create a temporary div to paste into
                const tempDiv = document.createElement('div');
                tempDiv.contentEditable = true;
                tempDiv.style.position = 'fixed';
                tempDiv.style.left = '-999px';
                tempDiv.style.top = '-999px';
                document.body.appendChild(tempDiv);
                
                tempDiv.focus();
                
                // Try to paste
                if (document.execCommand('paste')) {
                    clipboardData.html = tempDiv.innerHTML;
                    if (!clipboardData.text) {
                        clipboardData.text = tempDiv.innerText;
                    }
                }
                
                document.body.removeChild(tempDiv);
            } catch (e) {
                console.warn('Failed to use execCommand paste:', e);
            }
        }

        return clipboardData;
    } catch (error) {
        console.error('Clipboard access failed:', error);
        return { text: '', html: '' };
    }
};