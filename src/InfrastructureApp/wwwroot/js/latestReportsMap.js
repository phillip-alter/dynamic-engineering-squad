// SCRUM-98 ADDED:
// Loads only the location data for the selected report
// and renders a Google Map inside the Latest Reports modal.

document.addEventListener("DOMContentLoaded", function () {
    const reportModal = document.getElementById("reportModal");
    const reportItems = document.querySelectorAll(".report-item");
    const modalMapElement = document.getElementById("modalReportMap");
    const modalMapFallbackElement = document.getElementById("modalMapFallback");

    if (!reportModal || !modalMapElement || !modalMapFallbackElement) {
        return;
    }

    let pendingLatitude = null;
    let pendingLongitude = null;

    reportItems.forEach(function (item) {
        item.addEventListener("click", async function () {
            const reportId = this.dataset.reportid || "";

            
            pendingLatitude = null;
            pendingLongitude = null;
            modalMapElement.style.display = "none";
            modalMapElement.innerHTML = "";
            modalMapFallbackElement.classList.add("d-none");
            modalMapFallbackElement.textContent = "";

            if (!reportId) {
                return;
            }

            try {
                const response = await fetch(`/api/reports/${reportId}`);

                if (!response.ok) {
                    showMapFallback("Location could not be loaded.");
                    return;
                }

                const report = await response.json();
                pendingLatitude = report.latitude;
                pendingLongitude = report.longitude;
            }
            catch (error) {
                console.error("Error loading report location:", error);
                showMapFallback("Location could not be loaded.");
            }
        });
    });

    reportModal.addEventListener("shown.bs.modal", function () {
        renderMap(pendingLatitude, pendingLongitude);
    });

    function renderMap(latitude, longitude) {
        if (latitude == null || longitude == null || isNaN(latitude) || isNaN(longitude)) {
            showMapFallback("Location is not available for this report.");
            return;
        }

        if (typeof google === "undefined" || !google.maps) {
            showMapFallback("Map could not be loaded.");
            return;
        }

        modalMapFallbackElement.classList.add("d-none");
        modalMapFallbackElement.textContent = "";

        modalMapElement.style.display = "block";
        modalMapElement.innerHTML = "";

        const position = {
            lat: parseFloat(latitude),
            lng: parseFloat(longitude)
        };

        const map = new google.maps.Map(modalMapElement, {
            center: position,
            zoom: 15
        });

        new google.maps.Marker({
            position: position,
            map: map
        });
    }

    function showMapFallback(message) {
        modalMapElement.style.display = "none";
        modalMapFallbackElement.textContent = message;
        modalMapFallbackElement.classList.remove("d-none");
    }
});