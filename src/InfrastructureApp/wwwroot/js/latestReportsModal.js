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

    // -------------------------------------------------------
    // SCRUM-101 ADDED
    // URL link that opens the existing ReportIssue details page
    // Example: /ReportIssue/Details/5
    // -------------------------------------------------------
    const openFullReportLink = document.getElementById("openFullReportLink");

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // Keep track of the Latest Reports page URL so it can be
    // restored after the modal is closed.
    // This is the concept Chris suggested.
    // -------------------------------------------------------
    const latestReportsUrl = "/Reports/Latest";

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // Update the browser URL to match the selected report
    // without reloading the page.
    // -------------------------------------------------------
    function pushReportUrl(reportId) {
        if (!reportId) return;

        const newUrl = `/ReportIssue/Details/${reportId}`;

        // Avoid pushing duplicate history entries for the same URL
        if (window.location.pathname !== newUrl) {
            history.pushState({ reportId: reportId }, "", newUrl);
        }
    }

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // Restore browser URL back to the Latest Reports page
    // after the modal is closed.
    // -------------------------------------------------------
    function restoreLatestReportsUrl() {
        if (window.location.pathname !== latestReportsUrl) {
            history.replaceState({}, "", latestReportsUrl);
        }
    }

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // When the modal closes, restore the URL to /Reports/Latest
    // -------------------------------------------------------
    reportModal.addEventListener("hidden.bs.modal", function () {
        restoreLatestReportsUrl();
    });

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // If the user presses the browser Back button while the
    // modal is open, close the modal.
    // -------------------------------------------------------
    window.addEventListener("popstate", function () {
        const modalInstance = bootstrap.Modal.getInstance(reportModal);
        if (modalInstance) {
            modalInstance.hide();
        }
    });

    // Get all clickable report items
    const reportItems = document.querySelectorAll(".report-item");

    reportItems.forEach(function (item) {

        item.addEventListener("click", function () {

            const description = this.dataset.description || "";
            const created = this.dataset.created || "";
            const status = this.dataset.status || "";
            const imageUrl = this.dataset.image || ""; // read image URL from data-image attribute

            // -------------------------------------------------------
            // SCRUM-101 ADDED
            // Read report id so the modal can show a unique URL link
            // -------------------------------------------------------
            const reportId = this.dataset.reportid || "";

            // -------------------------------------------------------
            // SCRUM-101 ADDED
            // Point the modal link to Sunair's existing details page
            // -------------------------------------------------------
            if (openFullReportLink && reportId) {
                openFullReportLink.href = `/ReportIssue/Details/${reportId}`;
            }

            // -------------------------------------------------------
            // SCRUM-101 / pushState EXPERIMENT ADDED
            // Update the browser URL when the modal opens so the
            // selected report has a contextual deep link.
            // -------------------------------------------------------
            pushReportUrl(reportId);

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