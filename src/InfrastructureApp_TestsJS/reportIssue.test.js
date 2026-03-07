//jest tests the front end JS so it is included in the infrastructure app and not in infrastructureapp_tests 
//because that uses nUnit testing for the backend, whereas this tests the frontend logic

//js tests for create issue page JS
const {
    updateHiddenInputs,
    shouldBlockSubmit
} = require("../InfrastructureApp/wwwroot/js/reportIssue.js");

describe("Create Report JS Tests", () => {

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


//js tests for details map JS
const { initSubmittedMap } = require("../InfrastructureApp/wwwroot/js/reportIssue.js");

describe("Report Details JS Tests", () => {
  let mapCtor;
  let markerCtor;

  beforeEach(() => {
    // Reset DOM
    document.body.innerHTML = "";

    // Mock google maps
    mapCtor = jest.fn(() => ({ __map: true }));
    markerCtor = jest.fn(() => ({ __marker: true }));

    global.google = {
      maps: {
        Map: mapCtor,
        Marker: markerCtor,
      },
    };

    // Silence console output and allow assertions
    jest.spyOn(console, "warn").mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
    delete global.google;
  });

  test("returns early if submittedMap element is missing", () => {
    initSubmittedMap();

    expect(mapCtor).not.toHaveBeenCalled();
    expect(markerCtor).not.toHaveBeenCalled();
    expect(console.warn).not.toHaveBeenCalled();
  });

  test("warns and returns if coordinates are missing/invalid", () => {
    document.body.innerHTML = `
      <div id="submittedMap" data-lat="" data-lng=""></div>
    `;

    initSubmittedMap();

    expect(console.warn).toHaveBeenCalledWith("No coordinates available.");
    expect(mapCtor).not.toHaveBeenCalled();
    expect(markerCtor).not.toHaveBeenCalled();
  });

  test("creates a map and marker when coordinates are valid", () => {
    document.body.innerHTML = `
      <div id="submittedMap" data-lat="44.84845" data-lng="-123.23399"></div>
    `;

    initSubmittedMap();

    const mapElement = document.getElementById("submittedMap");
    const expectedPos = { lat: 44.84845, lng: -123.23399 };

    // Map created correctly
    expect(mapCtor).toHaveBeenCalledTimes(1);
    expect(mapCtor).toHaveBeenCalledWith(
      mapElement,
      expect.objectContaining({
        center: expectedPos,
        zoom: 16,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: false,
      })
    );

    // Marker created correctly
    const mapInstance = mapCtor.mock.results[0].value;
    expect(markerCtor).toHaveBeenCalledTimes(1);
    expect(markerCtor).toHaveBeenCalledWith({
      position: expectedPos,
      map: mapInstance,
    });

    expect(console.warn).not.toHaveBeenCalled();
  });

  test("treats 0/0 as invalid coordinates and warns", () => {
    document.body.innerHTML = `
      <div id="submittedMap" data-lat="0" data-lng="0"></div>
    `;

    initSubmittedMap();

    expect(console.warn).toHaveBeenCalledWith("No coordinates available.");
    expect(mapCtor).not.toHaveBeenCalled();
    expect(markerCtor).not.toHaveBeenCalled();
  });
});