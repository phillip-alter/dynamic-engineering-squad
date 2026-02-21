//jest tests the front end JS so it is included in the infrastructure app and not in infrastructureapp_tests 
//because that uses nUnit testing for the backend, whereas this tests the frontend logic


const {
    updateHiddenInputs,
    shouldBlockSubmit
} = require("../wwwroot/js/reportIssue.js");

describe("Report Issue JS", () => {

    beforeEach(() => {
        document.body.innerHTML = `
            <input id="Latitude" />
            <input id="Longitude" />
        `;
    });

    test("updates hidden inputs", () => {
        updateHiddenInputs(44.9, -123.0);

        expect(document.getElementById("Latitude").value)
            .toBe("44.9");

        expect(document.getElementById("Longitude").value)
            .toBe("-123");
    });

    test("blocks submit if location missing", () => {
        expect(shouldBlockSubmit("", "")).toBe(true);
    });

    test("allows submit with coordinates", () => {
        expect(shouldBlockSubmit(44.9, -123.0)).toBe(false);
    });

});