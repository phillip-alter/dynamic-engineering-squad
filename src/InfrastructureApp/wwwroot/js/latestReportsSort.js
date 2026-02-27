// SCRUM-86: Sorting behavior for Latest Reports (AJAX)
// This file triggers a reload when user changes OR clicks the sort dropdown.
// Clicking the already-selected option doesn't fire "change", so we also listen to "click".

document.addEventListener("DOMContentLoaded", () => {

  const sortSelect = document.getElementById("latestSortSelect");
  if (!sortSelect) return;

  // SCRUM-86 ADDED: reload reports using selected sort and current keyword
  async function reload() {

    const keywordInput = document.getElementById("latestSearchInput");
    const keyword = keywordInput ? keywordInput.value : "";

    const sort = sortSelect.value || "newest";

    // Uses the shared loader from latestReportsSearch.js
    if (window.loadLatestReports) {
      await window.loadLatestReports(keyword, sort);
    }
  }

  // Works when value changes
  sortSelect.addEventListener("change", reload);

  // SCRUM-86 FIX: reload even if same option is clicked
  sortSelect.addEventListener("click", reload);

});