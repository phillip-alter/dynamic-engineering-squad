// SCRUM-86 ADDED: Handles sorting dropdown changes and reloads reports using AJAX
document.addEventListener("DOMContentLoaded", () => {

    const sortSelect = document.getElementById("latestSortSelect");

    // exit if dropdown not present
    if (!sortSelect){
        return;
    } 

    sortSelect.addEventListener("change", async () => {

        const keywordInput = document.getElementById("latestSearchInput");

        const keyword = keywordInput ? keywordInput.value : "";
        const sort = sortSelect.value;

        // call existing search loader (SCRUM-83 function)
        if (window.loadLatestReports)
        {
            await window.loadLatestReports(keyword, sort);
        }

    });

});