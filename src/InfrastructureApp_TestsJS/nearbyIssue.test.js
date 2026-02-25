// nearbyIssues.test.js
describe("nearbyIssue.js", () => {
  beforeEach(() => {
    document.body.innerHTML = `
      <div id="statusMsg"></div>
      <div id="resultsList"></div>
      <div id="map"></div>
      <input id="radiusMiles" value="5" />
      <input id="locationInput" value="" />
      <button id="useMyLocationBtn"></button>
      <button id="searchLocationBtn"></button>
    `;

    window.__JEST__ = true;
    window.NearbyIssuesConfig = { nearbyApiUrl: "/api/reports/nearby", autoLocateOnLoad: false };

    global.fetch = jest.fn();

    // Minimal google maps mocks
    global.google = {
      maps: {
        Animation: { BOUNCE: "BOUNCE" },
        Map: jest.fn(() => ({
          setCenter: jest.fn(),
          setZoom: jest.fn(),
          getCenter: jest.fn(() => ({ lat: () => 10, lng: () => 20 })),
          fitBounds: jest.fn(),
        })),
        Marker: jest.fn(() => ({
          addListener: jest.fn(),
          setMap: jest.fn(),
          setAnimation: jest.fn(),
          getPosition: jest.fn(() => ({ lat: () => 10, lng: () => 20 })),
        })),
        InfoWindow: jest.fn(() => ({ setContent: jest.fn(), open: jest.fn() })),
        LatLngBounds: jest.fn(() => ({ extend: jest.fn() })),
      },
    };

    // Load the script (path depends on your project)
    jest.resetModules();
    require("../InfrastructureApp/wwwroot/js/nearbyIssue.js");
  });

  test("fetchNearby builds URL and returns JSON on success", async () => {
    fetch.mockResolvedValue({
      ok: true,
      json: async () => [{ id: 1 }],
    });

    const { _fetchNearby } = window.__nearbyIssues;
    const data = await _fetchNearby(44.0, -123.0, 5);

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch.mock.calls[0][0]).toContain("/api/reports/nearby?");
    expect(data).toEqual([{ id: 1 }]);
  });

  test("geocodeAddress throws readable status:errorMessage when API returns that shape", async () => {
    fetch.mockResolvedValue({
      ok: false,
      json: async () => ({ status: "ZERO_RESULTS", errorMessage: "No results found" }),
    });

    const { _geocodeAddress } = window.__nearbyIssues;
    await expect(_geocodeAddress("asdf")).rejects.toThrow("ZERO_RESULTS: No results found");
  });

  test("renderResults filters to approved only and updates status count", () => {
    const { _initMapAt, _renderResults } = window.__nearbyIssues;
    _initMapAt(1, 2);

    _renderResults([
      { id: 1, status: "Approved", latitude: 1, longitude: 2, createdAt: "2020-01-01" },
      { id: 2, status: "Pending", latitude: 1, longitude: 2, createdAt: "2020-01-01" },
      { id: 3, status: "ACTIVE", latitude: 1, longitude: 2, createdAt: "2020-01-01" },
    ]);

    expect(document.getElementById("statusMsg").textContent)
      .toContain("Found 2 approved report(s)");

    // Marker called once for search center + 2 approved reports
    expect(google.maps.Marker).toHaveBeenCalledTimes(3);

    // Sidebar has 2 items
    expect(document.getElementById("resultsList").children.length).toBe(2);
  });

  test("renderResults shows 'No approved reports' if none after filtering", () => {
    const { _initMapAt, _renderResults } = window.__nearbyIssues;
    _initMapAt(1, 2);

    _renderResults([
      { id: 1, status: "Pending", latitude: 1, longitude: 2, createdAt: "2020-01-01" },
    ]);

    expect(document.getElementById("statusMsg").textContent)
      .toBe("No approved reports found in this radius.");
  });
});