//this is the counter for the text area in the create report form that counts the number of character and displays them to the user. 300 characters max

document.addEventListener("DOMContentLoaded", () => {
    const ta = document.getElementById("Description");
    const count = document.getElementById("descCount");

    if (!ta || !count) return;

    const max = parseInt(ta.getAttribute("maxlength") || "0", 10);

    function update() {
        const len = ta.value.length;
        count.textContent = len.toString();
    }

    ta.addEventListener("input", update);
    update();
});
