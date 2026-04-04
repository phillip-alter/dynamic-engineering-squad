//this JS file watches user typing, calls the backend API, displays suggestions in a dropdown, lets the user interact with suggestions, updates only part of the text
//debouncing is used to prevent too many API calls, 
//it waits a little bit when you are typing a word to show you suggestions otherwise it would show them after every character
//Without it, every keystroke instantly fires a request, which is noisy and wasteful.




// Wait until the entire HTML page is loaded before running this script
document.addEventListener("DOMContentLoaded", () => {

    // Get the textarea where the user types the report description
    const descriptionInput = document.getElementById("Description");

    // Get the container where autocomplete suggestions will appear
    const suggestionsBox = document.getElementById("descriptionSuggestions");

    // If either element is missing, stop the script (prevents errors)
    if (!descriptionInput || !suggestionsBox) return;

    // This will hold the current list of suggestions returned from the server
    let suggestions = [];

    // Tracks which suggestion is currently highlighted (for keyboard navigation)
    // -1 means "nothing selected"
    let activeIndex = -1;

    // Used for debouncing (delaying API calls while typing)
    let debounceTimer = null;


    //EVENT: Runs every time the user types in the textarea
    descriptionInput.addEventListener("input", () => {

        // Cancel any previous delayed API call
        clearTimeout(debounceTimer);

        // Get the current text in the textarea
        const query = descriptionInput.value.trim();

        // If the user typed less than 2 characters, don't show suggestions
        if (query.length < 2) {
            hideSuggestions();
            return;
        }

        // Wait 200ms before calling the API (debouncing)
        debounceTimer = setTimeout(async () => {
            try {
                // Call your backend API with the current query
                const response = await fetch(`/api/reportassist/suggestions?q=${encodeURIComponent(query)}`);

                // If the API failed, hide suggestions
                if (!response.ok) {
                    hideSuggestions();
                    return;
                }

                // Convert the JSON response into a JS array
                suggestions = await response.json();

                // Reset keyboard selection
                activeIndex = -1;

                // Display suggestions in the dropdown
                renderSuggestions(suggestions);

            } catch {
                // If any error happens (network, etc.), hide suggestions
                hideSuggestions();
            }
        }, 200); // 200ms delay
    });


    //EVENT: Handles keyboard navigation inside the textarea
    descriptionInput.addEventListener("keydown", (e) => {

        // If there are no suggestions, do nothing
        if (suggestions.length === 0) return;

        // ↓ Arrow key → move selection down
        if (e.key === "ArrowDown") {
            e.preventDefault(); // prevent cursor movement

            // Move down but don't go past last item
            activeIndex = Math.min(activeIndex + 1, suggestions.length - 1);

            // Re-render to update highlighted item
            renderSuggestions(suggestions);

        // ↑ Arrow key → move selection up
        } else if (e.key === "ArrowUp") {
            e.preventDefault();

            // Move up but don't go below 0
            activeIndex = Math.max(activeIndex - 1, 0);

            renderSuggestions(suggestions);

        // Enter key → accept selected suggestion
        } else if (e.key === "Enter") {

            // Only apply suggestion if one is selected
            if (activeIndex >= 0) {
                e.preventDefault(); // prevent newline

                applySuggestion(suggestions[activeIndex]);
            }

        // Escape key → close dropdown
        } else if (e.key === "Escape") {
            hideSuggestions();
        }
    });


    //EVENT: Click anywhere on the page
    document.addEventListener("click", (e) => {

        // If click is outside the suggestions box AND not the textarea
        if (!suggestionsBox.contains(e.target) && e.target !== descriptionInput) {
            hideSuggestions();
        }
    });


    //FUNCTION: Render suggestions into the dropdown
    function renderSuggestions(items) {

        // Clear any existing suggestions
        suggestionsBox.innerHTML = "";

        // If no suggestions, hide dropdown
        if (!items || items.length === 0) {
            hideSuggestions();
            return;
        }

        // Loop through each suggestion
        items.forEach((item, index) => {

            // Create a div for each suggestion
            const div = document.createElement("div");

            // Add styling class + highlight if active
            div.className = "autocomplete-item" + (index === activeIndex ? " active" : "");

            // Set text content to the suggestion string
            div.textContent = item;

            // When user clicks (mousedown is used instead of click for reliability)
            div.addEventListener("mousedown", (e) => {
                e.preventDefault(); // prevent losing focus
                applySuggestion(item);
            });

            // Add this suggestion to the dropdown
            suggestionsBox.appendChild(div);
        });

        // Make dropdown visible
        suggestionsBox.classList.remove("d-none");
    }


    //FUNCTION: Apply a selected suggestion to the textarea
    function applySuggestion(suggestion) {

        // Get current text
        const currentText = descriptionInput.value;

        // Remove trailing spaces
        const trimmed = currentText.trimEnd();

        // Find where the last word starts
        const lastSpaceIndex = trimmed.lastIndexOf(" ");

        // Everything before the last word stays the same
        const prefix = lastSpaceIndex >= 0
            ? currentText.substring(0, lastSpaceIndex + 1)
            : "";

        // Replace only the last word/phrase with the suggestion
        descriptionInput.value = prefix + suggestion;

        // Update anything listening for typing, like word/character count
        descriptionInput.dispatchEvent(new Event("input"));

        // Hide dropdown after selection
        hideSuggestions();

        // Keep cursor focus in textarea
        descriptionInput.focus();
    }


    //FUNCTION: Hide and reset suggestions
    function hideSuggestions() {

        // Clear suggestions array
        suggestions = [];

        // Reset keyboard selection
        activeIndex = -1;

        // Remove all suggestion elements
        suggestionsBox.innerHTML = "";

        // Hide dropdown visually
        suggestionsBox.classList.add("d-none");
    }
});