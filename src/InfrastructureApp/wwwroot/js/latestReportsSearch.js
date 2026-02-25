// ----------------------------------------------------
// Latest Reports Search (AJAX)
// This script handles searching the Latest Reports page
// without refreshing the page. It calls our internal
// REST API endpoint and dynamically updates the list.
// ----------------------------------------------------

document.addEventListener("DOMContentLoaded", () => {

  // Get references to UI elements needed for search and results display
  const input = document.getElementById("latestSearchInput");
  const btn = document.getElementById("latestSearchButton");
  const clearBtn = document.getElementById("latestClearButton");
  const list = document.getElementById("latestReportsList");
  const msg = document.getElementById("latestSearchMessage");

  // Safety check: if any required element is missing, stop execution
  // This prevents JavaScript errors if the page structure changes
  if (!input || !btn || !list || !msg) return;


  // ----------------------------------------------------
  // Performs the search using AJAX (fetch)
  // Calls our ReportsAPIController endpoint and updates
  // the Latest Reports list dynamically without page reload
  // ----------------------------------------------------
  async function runSearch(keyword) {

    // Clear any previous messages before running new search
    msg.innerHTML = "";

    // Build the REST API URL with encoded keyword parameter
    const url = `/api/reports/latest?query=${encodeURIComponent(keyword ?? "")}`;

    // Call the backend API
    const res = await fetch(url);

    // If server returns an error, show friendly message
    if (!res.ok) {
      msg.innerHTML = `<div class="alert alert-danger">Sorry, an error occurred. Try again.</div>`;
      return;
    }

    // Convert the JSON response into JavaScript objects
    const reports = await res.json();

    // If no matching reports found, clear list and show message
    if (!reports || reports.length === 0) {
      list.innerHTML = "";
      msg.innerHTML = `<div class="alert alert-info">No matching reports found.</div>`;
      return;
    }

    // rebuild list buttons (keep same data-* so modal still works)
    list.innerHTML = reports.map(r => {
      const created = new Date(r.createdAt);
      const createdShort = created.toLocaleString();

      return `
        <button type="button"
          class="list-group-item list-group-item-action d-flex justify-content-between align-items-start report-item"
          data-bs-toggle="modal"
          data-bs-target="#reportModal"
          data-description="${escapeHtml(r.description ?? "")}"
          data-created="${escapeHtml(created.toLocaleString())}"
          data-status="${escapeHtml(r.status ?? "")}"
          data-image="${escapeHtml(r.imageUrl ?? "")}">
          <div class="me-3">
            <div class="fw-bold">${escapeHtml(r.description ?? "")}</div>
          </div>
          <small class="text-muted">${escapeHtml(createdShort)}</small>
        </button>
      `;
    }).join("");
  }

  btn.addEventListener("click", async () => {
    const keyword = input.value || "";
    await runSearch(keyword);
  });

  clearBtn.addEventListener("click", async () => {
    input.value = "";
    await runSearch("");
  });

  // Optional: press Enter to search
  input.addEventListener("keydown", async (e) => {
    if (e.key === "Enter") {
      e.preventDefault();
      await runSearch(input.value || "");
    }
  });
});

// basic HTML escaping so user text wont break DOM
function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}