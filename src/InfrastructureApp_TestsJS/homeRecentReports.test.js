const {
    buildRecentReportDetailsUrl,
    navigateToRecentReport
} = require("../InfrastructureApp/wwwroot/js/homeRecentReports");

describe("SCRUM-113 homeRecentReports.js", () => {
    let originalLocation;

    beforeEach(() => {
        originalLocation = window.location;
        delete window.location;
        window.location = { href: "" };
    });

    afterEach(() => {
        window.location = originalLocation;
    });

    test("buildRecentReportDetailsUrl returns correct details path", () => {
        const result = buildRecentReportDetailsUrl("25");

        expect(result).toBe("/ReportIssue/Details/25");
    });

    test("buildRecentReportDetailsUrl returns empty string when report id is missing", () => {
        expect(buildRecentReportDetailsUrl("")).toBe("");
        expect(buildRecentReportDetailsUrl(null)).toBe("");
        expect(buildRecentReportDetailsUrl(undefined)).toBe("");
    });

    test("navigateToRecentReport updates window location when report id exists", () => {
        const item = {
            dataset: {
                reportid: "42"
            }
        };

        navigateToRecentReport(item);

        expect(window.location.href).toBe("/ReportIssue/Details/42");
    });

    test("navigateToRecentReport does nothing when item is missing", () => {
        navigateToRecentReport(null);

        expect(window.location.href).toBe("");
    });

    test("navigateToRecentReport does nothing when report id is missing", () => {
        const item = {
            dataset: {
                reportid: ""
            }
        };

        navigateToRecentReport(item);

        expect(window.location.href).toBe("");
    });
});