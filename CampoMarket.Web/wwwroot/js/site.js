// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelectorAll(".cm-toast").forEach((element) => {
    bootstrap.Toast.getOrCreateInstance(element).show();
});

const confirmModalElement = document.getElementById("confirmModal");
const confirmModal = confirmModalElement
    ? bootstrap.Modal.getOrCreateInstance(confirmModalElement)
    : null;
const confirmModalTitle = document.getElementById("confirmModalLabel");
const confirmModalMessage = document.getElementById("confirmModalMessage");
const confirmModalAction = document.getElementById("confirmModalAction");
let pendingConfirmationForm = null;

document.addEventListener("submit", (event) => {
    const form = event.target.closest("form[data-confirm]");
    if (!form || form.dataset.confirmed === "true") {
        if (form) {
            delete form.dataset.confirmed;
        }
        return;
    }

    event.preventDefault();
    pendingConfirmationForm = form;

    confirmModalTitle.textContent = form.dataset.confirmTitle ?? "Confirmar acción";
    confirmModalMessage.textContent = form.dataset.confirm ?? "¿Deseas continuar?";
    confirmModalAction.textContent = form.dataset.confirmButton ?? "Confirmar";
    confirmModalAction.className = `btn ${form.dataset.confirmClass ?? "btn-primary"}`;
    confirmModal.show();
});

confirmModalAction?.addEventListener("click", () => {
    if (!pendingConfirmationForm) {
        return;
    }

    const form = pendingConfirmationForm;
    pendingConfirmationForm = null;
    form.dataset.confirmed = "true";
    confirmModal.hide();
    form.requestSubmit();
});

confirmModalElement?.addEventListener("hidden.bs.modal", () => {
    pendingConfirmationForm = null;
});
