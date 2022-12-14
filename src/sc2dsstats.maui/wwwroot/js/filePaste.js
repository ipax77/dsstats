function initializeFilePaste(fileDropContainer, inputFile) {

    function onPaste(e) {
        inputFile.files = e.clipboardData.files;
        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }

    fileDropContainer.addEventListener('paste', onPaste);

    return {
        dispose: () => {
            fileDropContainer.removeEventListener('paste', onPaste);
        }
    }
};

let button = document.querySelector('button');
let myImageBlob = "";

function initClipboard() {
    button = document.getElementById("importBtn");
    button.addEventListener('click', parseClipboardData());
}

async function parseClipboardData() {

    const items = await navigator.clipboard.read().catch((err) => {
        console.error(err);
    });

    for (let item of items) {
        for (let type of item.types) {
            if (type.startsWith("image/")) {
                const result = handleImage(type, item) || handleText(type, item);
                if (result) {
                    break;
                }
            }
        }
    }
};

function handleImage(type, clipboardItem) {
    const $container = document.querySelector(".container");
    if (type.startsWith("image/")) {
        clipboardItem.getType(type).then((imageBlob) => {
            myImageBlob = imageBlob;
            const image = `<img src="${window.URL.createObjectURL(imageBlob)}" />`;
            $container.innerHTML = image;
        });
        return true;
    }
    return false;
};

function getImageBlob() {
    return myImageBlob;
}

function initializeFileDropZone(dropZoneElement, inputFile) {
    // Add a class when the user drags a file over the drop zone
    function onDragHover(e) {
        e.preventDefault();
        dropZoneElement.classList.add("hover");
    }

    function onDragLeave(e) {
        e.preventDefault();
        dropZoneElement.classList.remove("hover");
    }

    // Handle the paste and drop events
    function onDrop(e) {
        e.preventDefault();
        dropZoneElement.classList.remove("hover");

        // Set the files property of the input element and raise the change event
        inputFile.files = e.dataTransfer.files;
        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }

    function onPaste(e) {
        // Set the files property of the input element and raise the change event
        inputFile.files = e.clipboardData.files;
        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }

    // Register all events
    dropZoneElement.addEventListener("dragenter", onDragHover);
    dropZoneElement.addEventListener("dragover", onDragHover);
    dropZoneElement.addEventListener("dragleave", onDragLeave);
    dropZoneElement.addEventListener("drop", onDrop);
    dropZoneElement.addEventListener('paste', onPaste);

    // The returned object allows to unregister the events when the Blazor component is destroyed
    return {
        dispose: () => {
            dropZoneElement.removeEventListener('dragenter', onDragHover);
            dropZoneElement.removeEventListener('dragover', onDragHover);
            dropZoneElement.removeEventListener('dragleave', onDragLeave);
            dropZoneElement.removeEventListener("drop", onDrop);
            dropZoneElement.removeEventListener('paste', onPaste);
        }
    }
}