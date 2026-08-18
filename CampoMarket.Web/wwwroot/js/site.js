// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelectorAll(".cm-toast").forEach((element) => {
    bootstrap.Toast.getOrCreateInstance(element).show();
});

document.addEventListener("submit", (event) => {
    const form = event.target.closest("form[data-confirm]");
    if (form && !window.confirm(form.dataset.confirm)) {
        event.preventDefault();
    }
});
