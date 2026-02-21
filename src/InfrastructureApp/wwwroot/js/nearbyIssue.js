//js for nearby issues map to see nearby issues within a radius
// Expects: window.NearbyIssuesConfig = { mapsKey: "...", nearbyApiUrl: "/api/reports/nearby", autoLocateOnLoad: true }

(() => {
  "use strict";

  let map;
  let markers = [];
  let infoWindow;

  function $(id) {
    return document.getElementById(id);
  }

  function setStatus(text) {
    const statusMsg = $("statusMsg");
    if (statusMsg) statusMsg.textContent = text || "";
  }

  function clearMarkers() {
    for (const m of markers) m.setMap(null);
    markers = [];
    const resultsList = $("resultsList");
    if (resultsList) resultsList.innerHTML = "";
  }

  function initMapAt(lat, lng, zoom = 13) {
    const center = { lat, lng };

    if (!map) {
      map = new google.maps.Map($("map"), { center, zoom });
      infoWindow = new google.maps.InfoWindow();
    } else {
      map.setCenter(center);
      map.setZoom(zoom);
    }

    // Show search center marker
    new google.maps.Marker({
      position: center,
      map,
      title: "Search center",
    });
  }

  async function fetchNearby(lat, lng, radiusMiles) {
    const cfg = window.NearbyIssuesConfig;
    const base = cfg?.nearbyApiUrl || "/api/nearbyIssues";

    const url = `${base}?lat=${encodeURIComponent(lat)}&lng=${encodeURIComponent(lng)}&radiusMiles=${encodeURIComponent(radiusMiles)}`;
    const res = await fetch(url);

    if (!res.ok) {
      const msg = await res.text();
      throw new Error(msg || "Failed to load nearby reports.");
    }

    return await res.json();
  }

  function renderResults(reports) {
  clearMarkers();

  if (!reports || reports.length === 0) {
    setStatus("No reports found in this radius.");
    return;
  }

  setStatus(`Found ${reports.length} report(s). Click a marker to view details.`);
  const resultsList = $("resultsList");

  for (const r of reports) {
    // Normalize casing (works with either camelCase or PascalCase JSON)
    const id = r.id ?? r.Id;
    const status = r.status ?? r.Status;
    const createdAt = r.createdAt ?? r.CreatedAt;
    const lat = r.latitude ?? r.Latitude;
    const lng = r.longitude ?? r.Longitude;
    const distanceMiles = r.distanceMiles ?? r.DistanceMiles;
    const detailsUrl = r.detailsUrl ?? r.DetailsUrl ?? `/ReportIssue/Details/${id}`;

    // Skip bad rows
    if (lat == null || lng == null) continue;

    const pos = { lat: Number(lat), lng: Number(lng) };

    const marker = new google.maps.Marker({
      position: pos,
      map,
      title: `#${id} - ${status}`,
    });

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

    markers.push(marker);

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
      a.addEventListener("mouseenter", () => marker.setAnimation(google.maps.Animation.BOUNCE));
      a.addEventListener("mouseleave", () => marker.setAnimation(null));
      resultsList.appendChild(a);
    }
  }

    // Fit bounds to markers (only if we have markers)
    if (markers.length > 0) {
        const bounds = new google.maps.LatLngBounds();
        for (const m of markers) bounds.extend(m.getPosition());
        map.fitBounds(bounds);
    } else {
        setStatus("No valid report coordinates returned.");
    }
}

  async function useBrowserLocation() {
    setStatus("Requesting location permission...");

    if (!navigator.geolocation) {
      setStatus("Geolocation not supported. Please enter a location.");
      return;
    }

    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        const lat = pos.coords.latitude;
        const lng = pos.coords.longitude;
        const radiusMiles = parseFloat($("radiusMiles")?.value || "5");

        initMapAt(lat, lng);
        setStatus("Loading nearby reports...");
        const reports = await fetchNearby(lat, lng, radiusMiles);
        renderResults(reports);
      },
      (err) => {
        console.warn(err);
        initMapAt(44.8512, -123.2334, 13); //default to monmouth if permission is denied
        setStatus("Location permission denied. Please enter a zip, city, or address and click Search.");
      },
      { enableHighAccuracy: true, timeout: 10000 }
    );
  }

  async function geocodeAddress(address) {
  const cfg = window.NearbyIssuesConfig;
  const key = cfg?.mapsKey;
  if (!key) throw new Error("Google Maps API key missing (NearbyIssuesConfig.mapsKey).");

  const url =
    `https://maps.googleapis.com/maps/api/geocode/json?address=${encodeURIComponent(address)}&region=us&key=${encodeURIComponent(key)}`;

  const res = await fetch(url);
  const data = await res.json();

  console.log("Geocode JSON:", data);

  if (data.status !== "OK" || !data.results?.length) {
    const msg = data.error_message
      ? `${data.status}: ${data.error_message}`
      : `${data.status}`;
    throw new Error(`Geocoding failed - ${msg}`);
  }

  const loc = data.results[0].geometry.location;
  return { lat: loc.lat, lng: loc.lng };
}

  async function searchLocation() {
    try {
        const address = ($("locationInput")?.value || "").trim();
        const radiusMiles = parseFloat($("radiusMiles")?.value || "5");

        if (!address) {
        setStatus("Enter a zip, city, or address first.");
        return;
        }

        setStatus("Finding that location...");
        const { lat, lng } = await geocodeAddress(address);

        initMapAt(lat, lng);
        setStatus("Loading nearby reports...");
        const reports = await fetchNearby(lat, lng, radiusMiles);
        renderResults(reports);
    } catch (e) {
        console.error(e);
        setStatus(e.message || "Search failed. Try a more specific address.");
    }
}

  // Called by Google Maps once its script is loaded
  window.initNearbyIssuesPage = () => {
    $("useMyLocationBtn")?.addEventListener("click", () => useBrowserLocation());
    $("searchLocationBtn")?.addEventListener("click", () => searchLocation());

    $("radiusMiles")?.addEventListener("change", async () => {
      if (!map) return;
      const c = map.getCenter();
      const radiusMiles = parseFloat($("radiusMiles")?.value || "5");

      setStatus("Refreshing nearby reports...");
      const reports = await fetchNearby(c.lat(), c.lng(), radiusMiles);
      renderResults(reports);
    });

    if (window.NearbyIssuesConfig?.autoLocateOnLoad !== false) {
      useBrowserLocation();
    } else {
      setStatus("Enter a location to search.");
    }
  };
})();