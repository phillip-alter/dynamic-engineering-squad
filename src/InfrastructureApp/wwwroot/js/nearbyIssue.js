//js for nearby issues map to see nearby issues within a radius
// Expects: window.NearbyIssuesConfig = { mapsKey: "...", nearbyApiUrl: "/api/reports/nearby", autoLocateOnLoad: true }
//loads Google Maps
//gets a search center (browser GPS or address search)
//calls your backend APIs:
    ///api/geocode?q=... (address → lat/lng)
    ///api/nearbyIssues?... (lat/lng → nearby reports)
//renders markers + a clickable list + an info window

(() => {
  "use strict";         // enables stricter JS rules (helps catch bugs)

  let map;              // Holds the Google Map instance
  let markers = [];     // We store all created markers so we can remove them later
  let infoWindow;       // Shared popup “speech bubble” shown when you click a marker

  function $(id) {
    return document.getElementById(id);
  }

  // Writes status text into an element with id="statusMsg"
  function setStatus(text) {
    const statusMsg = $("statusMsg");
    if (statusMsg) statusMsg.textContent = text || "";
  }

  // Removes all markers from the map and clears the results list
  function clearMarkers() {
    for (const m of markers) m.setMap(null);        // Remove each marker from the map
    markers = [];                                   // Reset marker array
    const resultsList = $("resultsList");           // Clear the HTML list in the sidebar (id="resultsList")
    if (resultsList) resultsList.innerHTML = "";
  }

  // Creates the map (first time) OR re-centers it (subsequent searches)
  function initMapAt(lat, lng, zoom = 13) {
    const center = { lat, lng };

    // First time: create Google map in element id="map"
    if (!map) {
      map = new google.maps.Map($("map"), { center, zoom });
      infoWindow = new google.maps.InfoWindow();        // Create a single reusable info window for all markers
    } else {
      map.setCenter(center);        // If map already exists: just update center + zoom
      map.setZoom(zoom);
    }

    // Show search center marker, NOT stored in `markers`, so it won't be removed by clearMarkers().
    new google.maps.Marker({
      position: center,
      map,
      title: "Search center",
    });
  }

  // Calls your backend API to get nearby issues
  async function fetchNearby(lat, lng, radiusMiles) {
    const cfg = window.NearbyIssuesConfig;
    const base = cfg?.nearbyApiUrl || "/api/nearbyIssues";      // If config not set, default to "/api/nearbyIssues"

    // Build query string for your API:
    const url = `${base}?lat=${encodeURIComponent(lat)}&lng=${encodeURIComponent(lng)}&radiusMiles=${encodeURIComponent(radiusMiles)}`;
    const res = await fetch(url);       // Make HTTP request to your ASP.NET API

    // If server returns 400/500 etc, throw an error for the caller to handle
    if (!res.ok) {
      const msg = await res.text();
      throw new Error(msg || "Failed to load nearby reports.");
    }

    // Parse JSON array returned from your API
    return await res.json();
  }

  // Renders the returned reports:
  // clears old markers/list
  // creates markers for each report
  // creates sidebar list entries
  // adds click handlers to show info windows
  function renderResults(reports) {
  clearMarkers();

  // Handle empty result set
  if (!reports || reports.length === 0) {
    setStatus("No reports found in this radius.");
    return;
  }

  // Only show APPROVED reports on the map/list (client-side filter)
  const approvedOnly = (reports || []).filter(r => {
    const status = (r.status ?? r.Status ?? "").toString().trim().toLowerCase();

    // Accept a few common variants just in case your DB/API uses them
    return status === "approved" || status === "approve" || status === "active";
  });

  // Handle empty result set AFTER filtering
  if (approvedOnly.length === 0) {
    setStatus("No approved reports found in this radius.");
    return;
  }


  setStatus(`Found ${approvedOnly.length} approved report(s). Click a marker to view details.`);
  const resultsList = $("resultsList");

  for (const r of approvedOnly) {
    // Your backend might return PascalCase properties (C# default) or camelCase 
    // Normalize casing (works with either camelCase or PascalCase JSON)
    const id = r.id ?? r.Id;
    const status = r.status ?? r.Status;
    const createdAt = r.createdAt ?? r.CreatedAt;
    const lat = r.latitude ?? r.Latitude;
    const lng = r.longitude ?? r.Longitude;
    const distanceMiles = r.distanceMiles ?? r.DistanceMiles;
    const detailsUrl = r.detailsUrl ?? r.DetailsUrl ?? `/ReportIssue/Details/${id}`;

    //Skip any report missing coordinates
    if (lat == null || lng == null) continue;
    
    // Google Maps expects numeric lat/lng
    const pos = { lat: Number(lat), lng: Number(lng) };

    // Create a marker for this report
    const marker = new google.maps.Marker({
      position: pos,
      map,
      title: `#${id} - ${status}`,
    });

    // When marker is clicked, show an info window popup with details + link
    marker.addListener("click", () => {
      const html = `
        <div style="min-width:220px">
          <div style="font-weight:600;margin-bottom:6px">Report #${id}</div>
          <div><b>Status:</b> ${status}</div>
          <div><b>Created:</b> ${new Date(createdAt).toLocaleString()}</div>
          ${distanceMiles != null ? `<div><b>Distance:</b> ${Number(distanceMiles).toFixed(2)} mi</div>` : ""}
          <div style="margin-top:8px">
            <a href="${detailsUrl}">View report</a>
          </div>
        </div>
      `;
      infoWindow.setContent(html);
      infoWindow.open(map, marker);
    });

    // Save marker so we can remove it later
    markers.push(marker);

    // Build sidebar list item (clickable link to details page)
    if (resultsList) {
      const a = document.createElement("a");
      a.href = detailsUrl;
      a.className = "list-group-item list-group-item-action";
      a.innerHTML = `
        <div class="d-flex justify-content-between">
          <span class="fw-semibold">#${id} - ${status}</span>
          ${distanceMiles != null ? `<span>${Number(distanceMiles).toFixed(2)} mi</span>` : ""}
        </div>
        <div class="text-muted">${new Date(createdAt).toLocaleString()}</div>
      `;

      //hovering the list item bounces the marker
      a.addEventListener("mouseenter", () => marker.setAnimation(google.maps.Animation.BOUNCE));
      a.addEventListener("mouseleave", () => marker.setAnimation(null));
      resultsList.appendChild(a);
    }
  }

    // After markers created, zoom/center map so all markers are visible
    if (markers.length > 0) {
        const bounds = new google.maps.LatLngBounds();
        for (const m of markers) bounds.extend(m.getPosition());
        map.fitBounds(bounds);
    } else {
        setStatus("No valid report coordinates returned.");
    }
}

    // Uses browser geolocation to set the search center automatically
    async function useBrowserLocation() {
        setStatus("Requesting location permission...");

        // If browser doesn't support geolocation, user must type a location
        if (!navigator.geolocation) {
        setStatus("Geolocation not supported. Please enter a location.");
        return;
        }

    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        const lat = pos.coords.latitude;
        const lng = pos.coords.longitude;
        const radiusMiles = parseFloat($("radiusMiles")?.value || "5");     // Read radius from UI input id="radiusMiles" (default 5 if missing)

        initMapAt(lat, lng);        // Initialize/recenter map at user location
        setStatus("Loading nearby reports...");     // Load nearby reports from your API and render
        const reports = await fetchNearby(lat, lng, radiusMiles);
        renderResults(reports);
      },
      (err) => {        // Error callback (permission denied, timeout, etc.)
        console.warn(err);
        initMapAt(44.8512, -123.2334, 13); //default to monmouth if permission is denied
        setStatus("Location permission denied. Please enter a zip, city, or address and click Search.");
      },
      { enableHighAccuracy: true, timeout: 10000 }      // Options: more accurate GPS, and a timeout
    );
  }

  // Calls your backend geocoding API:
  // /api/geocode?q=address
  // Returns { lat, lng }
  async function geocodeAddress(address) {
    const url = `/api/geocode?q=${encodeURIComponent(address)}`;
    const res = await fetch(url);

    // If geocoding fails, try to read JSON error and throw readable message
    if (!res.ok) {
        const data = await res.json().catch(() => null);
        const msg = data?.errorMessage
        ? `${data.status}: ${data.errorMessage}`
        : (data?.message || "Could not geocode that location.");
        throw new Error(msg);
    }

    return await res.json(); // { lat, lng }
    }

    // Uses typed address to find coordinates then load/render nearby issues
    async function searchLocation() {
        try {
            const address = ($("locationInput")?.value || "").trim();
            const radiusMiles = parseFloat($("radiusMiles")?.value || "5");

            // Require user to type something
            if (!address) {
                setStatus("Enter a zip, city, or address first.");
                return;
            }

            // Step 1: geocode address into lat/lng
            setStatus("Finding that location...");
            const { lat, lng } = await geocodeAddress(address);

            // Step 2: initialize map at those coordinates
            initMapAt(lat, lng);

            // Step 3: load nearby reports and render them
            setStatus("Loading nearby reports...");
            const reports = await fetchNearby(lat, lng, radiusMiles);
            renderResults(reports);
        } catch (e) {
            console.error(e);
            setStatus(e.message || "Search failed. Try a more specific address.");
        }
    }

  // Called by Google Maps once its script is loaded
  //<script ... callback=initNearbyIssuesPage> triggers it.
  window.initNearbyIssuesPage = () => {
    // Wire up button click handlers
    $("useMyLocationBtn")?.addEventListener("click", () => useBrowserLocation());
    $("searchLocationBtn")?.addEventListener("click", () => searchLocation());

    // When radius changes:
    // if map exists, use map center as search center
    // fetch + rerender reports
    $("radiusMiles")?.addEventListener("change", async () => {
      if (!map) return;

      const c = map.getCenter();
      const radiusMiles = parseFloat($("radiusMiles")?.value || "5");

      setStatus("Refreshing nearby reports...");
      const reports = await fetchNearby(c.lat(), c.lng(), radiusMiles);
      renderResults(reports);
    });

    //automatically ask for user location on page load
    if (window.NearbyIssuesConfig?.autoLocateOnLoad !== false) {
      useBrowserLocation();
    } else {
      setStatus("Enter a location to search.");
    }
  };
})();