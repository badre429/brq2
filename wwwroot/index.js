// @ts-check

var vueApp = {};
var map = null;
var rect = {
  p1: { x: 0, y: 0 },
  p2: { x: 0, y: 0 },
  started: false,
  isDown: false,
  serverDownloading: true,
  c1: { lat: 0, lng: 0 },
  c2: { lat: 0, lng: 0 },
};
function getStatus() {
  if (rect.serverDownloading) {
    fetch("/api/Tile/DownloadState").then((e) =>
      e.json().then((json) => {
        // console.log(json);
        vueApp.serverStat = json;
        vueApp.serverDownloading = true;
        rect.serverDownloading = json.isActive;
        vueApp.serverDownloading = json.isActive;
      })
    );
  }
  setTimeout(() => {
    getStatus();
  }, 1000);
}

function createSelectLayer() {
  map.addSource("selected-rect", {
    type: "geojson",
    data: {
      type: "FeatureCollection",
      features: [
        {
          type: "Feature",
          geometry: {
            type: "Polygon",
            coordinates: [
              [
                [-10, 37.5],
                [-10, 37.5],
                [-10, 37.5],
                [-10, 37.5],
                [-10, 37.5],
              ],
            ],
          },
        },
      ],
    },
  });
  map.addLayer({
    id: "selected-rect",
    type: "fill",
    source: "selected-rect",
    paint: {
      "fill-color": "#0000FF",
      "fill-opacity": 0.2,
    },
    layout: {
      visibility: "visible",
    },
  });
  map.addLayer({
    id: "selected-rect-outline",
    type: "line",
    source: "selected-rect",
    paint: {
      "line-color": "#0000FF",
      "line-width": 2,
    },
    layout: {
      visibility: "visible",
    },
  });
}

getStatus();
function createMapStyle(p) {
  return {
    version: 8,
    glyphs: "/outlib/glyphs/{fontstack}-{range}.pbf",
    sources: {
      "simple-tiles": {
        type: "raster",
        tiles: ["/tile/{z}/{x}/{y}?layerId=" + p.layers[0].id],
        tileSize: 256,
      },
    },
    layers: [
      {
        id: "simple-tiles",
        type: "raster",
        source: "simple-tiles",
        minzoom: 0,
        maxzoom: 22,
      },
    ],
  };
}
function getLayerFrom(v, layer) {
  if (v == null) {
    return "/mapstyles/" + layer + ".json";
  } else {
    var style = createMapStyle(v);
    // console.log('raster data type', this.layer, v, style);
    return style;
  }
}
function CreateLayer(prv) {
  // @ts-ignore
  var layer = prv.layer[0];
}
function updateSelectedLayer() {
  var StartLat = Math.max(rect.c1.lat, rect.c2.lat);
  var EndLat = Math.min(rect.c1.lat, rect.c2.lat);
  var StartLng = Math.min(rect.c1.lng, rect.c2.lng);
  var EndLng = Math.max(rect.c1.lng, rect.c2.lng);
  var data = {
    type: "FeatureCollection",
    features: [
      {
        type: "Feature",
        geometry: {
          type: "Polygon",
          coordinates: [
            [
              [StartLng, StartLat],
              [StartLng, EndLat],
              [EndLng, EndLat],
              [EndLng, StartLat],
              [StartLng, StartLat],
            ],
          ],
        },
      },
    ],
  };
  map.getSource("selected-rect").setData(data);
}
function DrawPointsLayer() {
  map.addSource("geojsonPoints", {
    type: "geojson",
    data: "/geo.json",
  });
  map.addLayer({
    id: "points",
    type: "circle",
    source: "geojsonPoints",
    paint: {
      "circle-radius": 5,
      "circle-color": [
        "case",
        ["==", ["get", "country"], "DZ"],
        "green",
        "blue",
      ],
    },
  });
  map.addLayer({
    id: "labelspoints",
    type: "symbol",
    source: "geojsonPoints",
    layout: {
      "text-font": ["Arial"],
      "text-size": {
        base: 1.2,
        stops: [
          [7, 12],
          [11, 19],
        ],
      },
      "text-offset": [0, 0.5],
      "text-anchor": "top",
      "text-field": {
        stops: [
          [0, ""],
          [7, "{name}"],
        ],
      },
    },
    paint: {
      "text-color": "#0000ff",
      "text-halo-width": 3,
      "text-halo-blur": 2,
      "text-halo-color": "rgba(255,255,255,0.8)",
    },
  });
}
fetch("/api/Tile/Providers").then((e) =>
  e.json().then((c) => {
    mapboxgl.accessToken =
      "pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA";
    var style = null;
    if (localStorage.getItem("selected-layer") != null) {
      var v = c.find(
        (k) =>
          k.name == localStorage.getItem("selected-layer") && k.type == "raster"
      );
      style = getLayerFrom(v, localStorage.getItem("selected-layer"));
    } else style = "/mapstyles/Mapbox.json";
    var l = null;
    if (localStorage.getItem("oldMap")) {
      var l = JSON.parse(localStorage.getItem("oldMap"));
    }
    map = new mapboxgl.Map({
      container: "main-map", // container id
      // @ts-ignore
      style: style,
      center: l ? [l.lng, l.lat] : [3.5, 29], // starting position
      zoom: l ? l.zoom : 4.5, // starting zoom
    });
    // @ts-ignore
    map.on("load", (k) => {
      createSelectLayer();
      if (localStorage.getItem("oldMap")) {
        var l = JSON.parse(localStorage.getItem("oldMap"));
      }
    });
    map.on("mousedown", (k) => {
      rect.c1 = k.lngLat;
      var e = k.originalEvent;
      if (rect.started && !rect.isDown && e.button == 0) {
        rect.isDown = true;
        // @ts-ignore
        var elm = document.getElementById("select-rect");
        // elm.style.display = 'unset';
        if (map.getLayer("selected-rect-outline") == null) {
          createSelectLayer();
        }
        map
          .getLayer("selected-rect-outline")
          .setLayoutProperty("visibility", "visible");
        map
          .getLayer("selected-rect")
          .setLayoutProperty("visibility", "visible");
        rect.p1 = { x: e.clientX, y: e.clientY };
        map.dragPan.disable();
        //   console.log(e);
      }
    });
    // @ts-ignore
    window.addEventListener("mouseup", (k) => {
      if (rect.started && rect.isDown) {
        rect.isDown = false;
        vueApp.rect = rect;
        vueApp.regionSelected = true;

        map.dragPan.enable();

        updateSelectedLayer();
      }
    });

    map.on("mousemove", (k) => {
      // @ts-ignore
      var e = k.originalEvent;
      if (rect.started && rect.isDown) {
        rect.c2 = k.lngLat;
        //   console.log(e);

        if (map.getLayer("selected-rect-outline") == null) {
          createSelectLayer();
        }
        map
          .getLayer("selected-rect-outline")
          .setLayoutProperty("visibility", "visible");
        map
          .getLayer("selected-rect")
          .setLayoutProperty("visibility", "visible");
        updateSelectedLayer();
      }
    });
    // Add zoom and rotation controls to the map.

    map.on("moveend", () => {
      var l = map.getCenter();
      l.zoom = map.getZoom();
      console.log(l);
      localStorage.setItem("oldMap", JSON.stringify(l));
    });
    map.addControl(new mapboxgl.NavigationControl());
    map.addControl(new mapboxgl.ScaleControl());
    vueApp = new Vue({
      el: "#main-toolbar",
      // @ts-ignore
      vuetify: new Vuetify(),
      data: {
        layer: localStorage.getItem("selected-layer") || "Mapbox",
        items: [...c],
        selection: false,
        serverDownloading: false,
        cancelMessage: false,
        startedMessage: false,
        regionSelected: false,
        serverStat: {
          zoom: 0,
          count: 0,
          total: 0,
        },
        rect: {},
      },
      methods: {
        // @ts-ignore
        startSelection: function (e) {
          this.regionSelected = false;
          var elm = document.getElementById("select-rect");
          // elm.style.display = 'none';

          if (map.getLayer("selected-rect-outline") == null) {
            createSelectLayer();
          }
          // map.getLayer('selected-rect').setLayoutProperty('visibility', 'none');
          // map
          //   .getLayer('selected-rect-outline')
          //   .setLayoutProperty('visibility', 'none');
          elm.style.height = "0px";
          elm.style.width = "0px";
          rect.started = true;
          this.selection = true;
          // console.log(this);
        },
        DrawPointsLayer: function () {
          DrawPointsLayer();
        },
        // @ts-ignore
        endSelection: function (e) {
          // document.getElementById('select-rect').style.display = 'none';
          rect.started = false;
          this.selection = false;
          // console.log(this);
        },

        // @ts-ignore
        layerChanged: function (e) {
          localStorage.setItem("selected-layer", this.layer);
          var v = c.find((k) => k.name == this.layer && k.type == "raster");

          map.setStyle(getLayerFrom(v, this.layer));
        },
        startDownloading: function () {
          var self = this;
          var v = c.find((k) => k.name == this.layer);
          rect.providerId = v.id;
          rect.startZoom = Math.floor(map.getZoom());
          rect.endZoom = 17;
          delete rect["points"];
          fetch("/api/Tile/StartDownloading", {
            method: "POST",
            headers: {
              Accept: "application/json",
              "Content-Type": "application/json",
            },
            body: JSON.stringify(rect),
            // @ts-ignore
          }).then((k) => {
            rect.serverDownloading = true;
            self.serverDownloading = true;
          });
        },
        startDownloadingPoints: function () {
          var self = this;
          var v = c.find((k) => k.name == this.layer);
          rect.providerId = v.id;
          rect.startZoom = Math.floor(map.getZoom());
          rect.endZoom = 17;
          fetch("/geo.json").then((k) =>
            k.json().then((data) => {
              console.log(data);
              rect.points = data.features
                .map((k) => k.geometry.coordinates)
                .map((cr) => ({ lat: cr[1], lng: cr[0] }))
                .filter((cr) => cr.lat != null && cr.lng != null);
              console.log(rect);
              fetch("/api/Tile/StartDownloading", {
                method: "POST",
                headers: {
                  Accept: "application/json",
                  "Content-Type": "application/json",
                },
                body: JSON.stringify(rect),
                // @ts-ignore
              }).then((k) => {
                rect.serverDownloading = true;
                self.serverDownloading = true;
              });
            })
          );
        },
        updateSelectedLayer: function () {
          updateSelectedLayer();
        },
        stopDownloading: function () {
          var self = this;
          // @ts-ignore
          fetch("/api/Tile/DownloadCancel").then((k) => {
            self.cancelMessage = true;
            rect.serverDownloading = false;
            self.serverDownloading = false;
          });
        },
      },
    });

    // @ts-ignore
    var draw = new MapboxDraw({
      // displayControlsDefault: false,
      // controls: {
      //   polygon: true,
      //   trash: true,
      // },
    });
    map.on("moveend", () => {
      var l = map.getCenter();
      l.zoom = map.getZoom();
      console.log(l);
      localStorage.setItem("mapOld", JSON.stringify(l));
    });
    map.addControl(draw, "bottom-right");
    map.on("draw.create", updateArea);
    map.on("draw.delete", updateArea);
    map.on("draw.update", updateArea);
    // map.on("draw.actionable", updateArea);
    // map.on("click", (e) => updateArea(e, true));
    var isDrawwing = 0;
    function updateArea(e, click) {
      var data = draw.getAll();
      console.log(data);
      var answer = document.getElementById("calculated-area");
      if (data.features.length > 0) {
        var feat = data.features[data.features.length - 1];
        if (feat.geometry.type == "Polygon") {
          // @ts-ignore
          var area = turf.area(data);
          // restrict to area to 2 decimal points
          var rounded_area = Math.round(area * 100) / 100;
          answer.innerHTML =
            "<p><strong>surface:" +
            rounded_area +
            "</strong></p><p>square meters</p>";
        } else if (feat.geometry.type == "LineString") {
          // @ts-ignore
          var area = turf.length(data);
          // restrict to area to 2 decimal points
          var rounded_area = Math.round(area * 1000) / 1000;
          answer.innerHTML =
            "<p><strong>distance" + rounded_area + "</strong></p><p> km</p>";
        }
      } else {
        answer.innerHTML = "";
        if (e.type !== "draw.delete")
          alert("Use the draw tools to draw a polygon!");
      }
    }
  })
);
