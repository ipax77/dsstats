
function enableTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))
}

function ReplayModalOpen(name) {

    const modalElement = document.getElementById(name);

    if (!modalElement) {
        return;
    }

    replayModal = new bootstrap.Modal(modalElement);
    replayModal.show();
}

function ReplayModalClose() {
    if (replayModal) {
        replayModal.hide();
    }
}

function setMultiSelect(id, selectedValues) {
    var multiSelect = document.querySelector('#' + id);
    var options = Array.from(multiSelect.options);
    options.forEach(function (option) {
        if (selectedValues.includes(option.value)) {
            option.selected = true;
        } else {
            option.selected = false;
        }
    });
}