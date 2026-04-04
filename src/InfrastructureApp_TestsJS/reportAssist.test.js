
/**
 * @jest-environment jsdom
 */

describe("reportAssist.js", () => {
    let descriptionInput;
    let suggestionsBox;

    function loadScript() {
        require("../InfrastructureApp/wwwroot/js/reportAssist.js");
    }

    beforeEach(() => {
        jest.resetModules();
        jest.useFakeTimers();

        document.body.innerHTML = `
            <textarea id="Description"></textarea>
            <div id="descriptionSuggestions" class="d-none"></div>
        `;

        descriptionInput = document.getElementById("Description");
        suggestionsBox = document.getElementById("descriptionSuggestions");

        global.fetch = jest.fn();

        const originalAddEventListener = document.addEventListener.bind(document);

        jest.spyOn(document, "addEventListener").mockImplementation((type, listener, options) => {
            if (type === "DOMContentLoaded") {
                listener(new Event("DOMContentLoaded"));
                return;
            }

            return originalAddEventListener(type, listener, options);
        });

        loadScript();
    });

    afterEach(() => {
        jest.clearAllMocks();
        jest.clearAllTimers();
        jest.useRealTimers();
        jest.restoreAllMocks();
    });


    test("does nothing if query length is less than 2", () => {
        descriptionInput.value = "a";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(250);

        expect(fetch).not.toHaveBeenCalled();
        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    test("calls API after debounce and renders suggestions", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole", "pole damage"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        // debounce delay
        jest.advanceTimersByTime(200);

        // let async fetch/json finish
        await Promise.resolve();
        await Promise.resolve();

        expect(fetch).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith("/api/reportassist/suggestions?q=po");

        const items = suggestionsBox.querySelectorAll(".autocomplete-item");
        expect(items.length).toBe(2);
        expect(items[0].textContent).toBe("pothole");
        expect(items[1].textContent).toBe("pole damage");
        expect(suggestionsBox.classList.contains("d-none")).toBe(false);
    });

    test("debounce prevents multiple rapid API calls", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "p";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        descriptionInput.value = "pot";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);

        await Promise.resolve();
        await Promise.resolve();

        expect(fetch).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith("/api/reportassist/suggestions?q=pot");
    });

    test("hides suggestions if API response is not ok", async () => {
        fetch.mockResolvedValue({
            ok: false
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);

        await Promise.resolve();

        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    test("hides suggestions if fetch throws an error", async () => {
        fetch.mockRejectedValue(new Error("network error"));

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);

        await Promise.resolve();

        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    test("ArrowDown highlights the first suggestion", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole", "pole damage"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true })
        );

        const items = suggestionsBox.querySelectorAll(".autocomplete-item");
        expect(items[0].classList.contains("active")).toBe(true);
        expect(items[1].classList.contains("active")).toBe(false);
    });

    test("ArrowDown then ArrowUp updates highlighted suggestion", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole", "pole damage"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true })
        );
        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true })
        );
        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowUp", bubbles: true })
        );

        const items = suggestionsBox.querySelectorAll(".autocomplete-item");
        expect(items[0].classList.contains("active")).toBe(true);
        expect(items[1].classList.contains("active")).toBe(false);
    });

    test("Enter applies the active suggestion and replaces only the last word", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "big pot";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true })
        );

        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "Enter", bubbles: true })
        );

        expect(descriptionInput.value).toBe("big pothole");
        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    test("mousedown on suggestion applies it", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["streetlight"]
        });

        descriptionInput.value = "broken stre";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        const item = suggestionsBox.querySelector(".autocomplete-item");
        item.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));

        expect(descriptionInput.value).toBe("broken streetlight");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    test("Escape hides suggestions", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "Escape", bubbles: true })
        );

        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    test("clicking outside hides suggestions", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        const outside = document.createElement("div");
        document.body.appendChild(outside);

        outside.dispatchEvent(new MouseEvent("click", { bubbles: true }));

        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    test("clicking inside textarea does not hide suggestions", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        descriptionInput.dispatchEvent(new MouseEvent("click", { bubbles: true }));

        expect(suggestionsBox.querySelectorAll(".autocomplete-item").length).toBe(1);
        expect(suggestionsBox.classList.contains("d-none")).toBe(false);
    });
});