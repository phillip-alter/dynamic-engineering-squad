// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Feature-81: Latest Reports modal population 
document.addEventListener("DOMContentLoaded", function () {

    // Only run on pages that contain the modal
    const reportModal = document.getElementById("reportModal");
    if (!reportModal) {
        return;
    }

    // Modal content elements
    const modalDescriptionElement = document.getElementById("modalDescription");
    const modalCreatedElement = document.getElementById("modalCreated");
    const modalStatusElement = document.getElementById("modalStatus");
    // Modal image elements
    const modalImageElement = document.getElementById("modalImage");
    const modalImageFallbackElement = document.getElementById("modalImageFallback");

    // Get all clickable report items
    const reportItems = document.querySelectorAll(".report-item");

    reportItems.forEach(function (item) {

        item.addEventListener("click", function () {

            const description = this.dataset.description || "";
            const created = this.dataset.created || "";
            const status = this.dataset.status || "";
            const imageUrl = this.dataset.image || ""; // read image URL from data-image attribute
 
            if (modalDescriptionElement) {
                modalDescriptionElement.textContent = description;
            }

            if (modalCreatedElement) {
                modalCreatedElement.textContent = created;
            }

            if (modalStatusElement) {
                modalStatusElement.textContent = status;
            }

            // Image handling logic 
            if (modalImageElement && modalImageFallbackElement) {

                if (imageUrl.trim().length > 0) {

                    // Show image if URL exists
                    modalImageElement.src = imageUrl;
                    modalImageElement.classList.remove("d-none");

                    modalImageFallbackElement.classList.add("d-none");
                    modalImageFallbackElement.textContent = "";
                }
                else {
                    // Show fallback message if no image
                    modalImageElement.removeAttribute("src");
                    modalImageElement.classList.add("d-none");

                    modalImageFallbackElement.textContent = "No image was provided for this report.";
                    modalImageFallbackElement.classList.remove("d-none");
                }
            }
        });
    });
});