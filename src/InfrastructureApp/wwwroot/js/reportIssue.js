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



// Handles Google Maps interaction for reporting issues

let map;
let marker = null;

// Google Maps calls this automatically because of callback=initMap
//window.initMap makes it global and allows it to work with modules/bundling
window.initMap = function initMap() {

    // Default location (Monmouth, Oregon)
    const defaultLocation = {
        lat: 44.84845,
        lng: -123.23399
    };

    // Create map
    map = new google.maps.Map(document.getElementById("map"), {
        center: defaultLocation,
        zoom: 12,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: false
    });

    // When user clicks map
    map.addListener("click", function (event) {

        const lat = event.latLng.lat();
        const lng = event.latLng.lng();

        placeMarker(lat, lng);
        updateHiddenInputs(lat, lng);
    });
}



// Marker logic
function placeMarker(lat, lng) {

    const position = { lat: lat, lng: lng };

    if (marker === null) {
        marker = new google.maps.Marker({
            position: position,
            map: map
        });
    } else {
        marker.setPosition(position);
    }
}


// Hidden form fields
function updateHiddenInputs(lat, lng) {

    const latInput = document.getElementById("Latitude");
    const lngInput = document.getElementById("Longitude");

    if (latInput && lngInput) {
        latInput.value = lat;
        lngInput.value = lng;
    }
}

//block submission if no geolocation provided
function shouldBlockSubmit(lat, lng) {
    // Treat empty strings, null, undefined as invalid
    return lat == null || lat === "" || lng == null || lng === "";
}


// Prevent submit without location
document.addEventListener("DOMContentLoaded", () => {

    const form = document.querySelector("form");

    if (!form) return;

    form.addEventListener("submit", function (e) {

        const lat = document.getElementById("Latitude").value;
        const lng = document.getElementById("Longitude").value;

        if (shouldBlockSubmit(lat, lng)) {
            e.preventDefault();
            alert("Please select the location of the issue.");
        }
    });
});



// map for details.cshtml

window.initSubmittedMap = function () {

    const mapElement = document.getElementById("submittedMap");
    if (!mapElement) return;

    const lat = Number(mapElement.dataset.lat);
    const lng = Number(mapElement.dataset.lng);

    if (!lat || !lng) {
        console.warn("No coordinates available.");
        return;
    }

    const position = { lat: lat, lng: lng };

    const map = new google.maps.Map(mapElement, {
        center: position,
        zoom: 16,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: false
    });

    new google.maps.Marker({
        position: position,
        map: map
    });
};






// Export functions for Jest testing
if (typeof module !== "undefined") {
    module.exports = {
        updateHiddenInputs,
        shouldBlockSubmit
    };
}







