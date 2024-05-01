export function initializeFilePaste(fileDropContainer, inputFile) {
    function onPaste(event) {
        inputFile.files = event.clipboardData.files;
        const changeEvent = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(changeEvent);
    }

    fileDropContainer.addEventListener('paste', onPaste);
    return {
        dispose: () => {
            fileDropContainer.removeEventListener('paste', onPaste);
        }
    }
}

