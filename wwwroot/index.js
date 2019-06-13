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
  c2: { lat: 0, lng: 0 }
};
function getStatus() {
  if (rect.serverDownloading) {
    fetch('/api/Tile/DownloadState').then(e =>
      e.json().then(json => {
        // console.log(json);
        vueApp.serverStat = json;
        vueApp.serverDownloading = true;
        rect.serverDownloading = json.ù;
        vueApp.serverDownloading = json.isActive;
      })
    );
  }
  setTimeout(() => {
    getStatus();
  }, 1000);
}

function createSelectLayer() {
  map.addSource('selected-rect', {
    type: 'geojson',
    data: {
      type: 'FeatureCollection',
      features: [
        {
          type: 'Feature',
          geometry: {
            type: 'Polygon',
            // coordinates: [[[-10, 37.5], [-10, 18], [18, 18], [18, 37.5], [-10, 37.5]]]
            coordinates: [[[-10, 37.5], [-10, 37.5], [-10, 37.5], [-10, 37.5], [-10, 37.5]]]
          }
        }
      ]
    }
  });
  map.addLayer({
    id: 'selected-rect',
    type: 'fill',
    source: 'selected-rect',
    paint: {
      'fill-color': '#0000FF',
      'fill-opacity': 0.2
    },
    layout: {
      visibility: 'visible'
    }
  });
  map.addLayer({
    id: 'selected-rect-outline',
    type: 'line',
    source: 'selected-rect',
    paint: {
      'line-color': '#0000FF',
      'line-width': 2
    },
    layout: {
      visibility: 'visible'
    }
  });
}

getStatus();
function createMapStyle(p) {
  return {
    version: 8,
    // glyphs: '/api/{fontstack}-{range}.pbf',
    sources: {
      'simple-tiles': {
        type: 'raster',
        tiles: ['/tile/{z}/{x}/{y}?layerId=' + p.layers[0].id],
        tileSize: 256
      }
    },
    layers: [
      {
        id: 'simple-tiles',
        type: 'raster',
        source: 'simple-tiles',
        minzoom: 0,
        maxzoom: 22
      }
    ]
  };
}
function CreateLayer(prv) {
  var layer = prv.layer[0];
}
function updateSelectedLayer() {
  var StartLat = Math.max(rect.c1.lat, rect.c2.lat);
  var EndLat = Math.min(rect.c1.lat, rect.c2.lat);
  var StartLng = Math.min(rect.c1.lng, rect.c2.lng);
  var EndLng = Math.max(rect.c1.lng, rect.c2.lng);
  var data = {
    type: 'FeatureCollection',
    features: [
      {
        type: 'Feature',
        geometry: {
          type: 'Polygon',
          coordinates: [
            [
              [StartLng, StartLat],
              [StartLng, EndLat],
              [EndLng, EndLat],
              [EndLng, StartLat],
              [StartLng, StartLat]
            ]
          ]
        }
      }
    ]
  };
  map.getSource('selected-rect').setData(data);
}
fetch('/api/Tile/Providers').then(e =>
  e.json().then(c => {
    console.log(c);
    mapboxgl.accessToken =
      'pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA';
    map = new mapboxgl.Map({
      container: 'main-map', // container id
      style: '/mapstyles/Mapbox.json',
      center: [3.5, 29], // starting position
      zoom: 4.5 // starting zoom
    });
    map.on('load', k => {
      createSelectLayer();
    });
    map.on('mousedown', k => {
      rect.c1 = k.lngLat;
      var e = k.originalEvent;
      if (rect.started && !rect.isDown && e.button == 0) {
        rect.isDown = true;
        var elm = document.getElementById('select-rect');
        // elm.style.display = 'unset';
        if (map.getLayer('selected-rect-outline') == null) {
          createSelectLayer();
        }
        map
          .getLayer('selected-rect-outline')
          .setLayoutProperty('visibility', 'visible');
        map
          .getLayer('selected-rect')
          .setLayoutProperty('visibility', 'visible');
        rect.p1 = { x: e.clientX, y: e.clientY };
        map.dragPan.disable();
        //   console.log(e);
      }
    });
    window.addEventListener('mouseup', k => {
      if (rect.started && rect.isDown) {
        rect.isDown = false;
        vueApp.rect = rect;
        vueApp.regionSelected = true;

        map.dragPan.enable();

        updateSelectedLayer();
      }
    });

    map.on('mousemove', k => {
      var e = k.originalEvent;
      if (rect.started && rect.isDown) {
        rect.c2 = k.lngLat;
        //   console.log(e);

        if (map.getLayer('selected-rect-outline') == null) {
          createSelectLayer();
        }
        map
          .getLayer('selected-rect-outline')
          .setLayoutProperty('visibility', 'visible');
        map
          .getLayer('selected-rect')
          .setLayoutProperty('visibility', 'visible');
        updateSelectedLayer();
      }
    });
    // Add zoom and rotation controls to the map.
    map.addControl(new mapboxgl.NavigationControl());
    vueApp = new Vue({
      el: '#main-toolbar',
      data: {
        layer: null,
        items: c,
        selection: false,
        serverDownloading: false,
        cancelMessage: false,
        startedMessage: false,
        regionSelected: false,
        serverStat: {
          zoom: 0,
          count: 0,
          total: 0
        },
        rect: {}
      },
      methods: {
        startSelection: function(e) {
          this.regionSelected = false;
          var elm = document.getElementById('select-rect');
          // elm.style.display = 'none';

          if (map.getLayer('selected-rect-outline') == null) {
            createSelectLayer();
          }
          // map.getLayer('selected-rect').setLayoutProperty('visibility', 'none');
          // map
          //   .getLayer('selected-rect-outline')
          //   .setLayoutProperty('visibility', 'none');
          elm.style.height = '0px';
          elm.style.width = '0px';
          rect.started = true;
          this.selection = true;
          // console.log(this);
        },
        endSelection: function(e) {
          // document.getElementById('select-rect').style.display = 'none';
          rect.started = false;
          this.selection = false;
          // console.log(this);
        },

        layerChanged: function(e) {
          var v = c.find(k => k.name == this.layer && k.type == 'raster');
          if (v == null) {
            map.setStyle('/mapstyles/' + this.layer + '.json');
          } else {
            var style = createMapStyle(v);
            // console.log('raster data type', this.layer, v, style);
            map.setStyle(style);
          }
        },
        startDownloading: function() {
          var self = this;
          var v = c.find(k => k.name == this.layer);
          rect.providerId = v.id;
          rect.startZoom = Math.floor(map.getZoom());
          rect.endZoom = 20;
          fetch('/api/Tile/StartDownloading', {
            method: 'POST',
            headers: {
              Accept: 'application/json',
              'Content-Type': 'application/json'
            },
            body: JSON.stringify(rect)
          }).then(k => {
            rect.serverDownloading = true;
            self.serverDownloading = true;
          });
        },
        updateSelectedLayer: function() {
          updateSelectedLayer();
        },
        stopDownloading: function() {
          var self = this;
          fetch('/api/Tile/DownloadCancel').then(k => {
            self.cancelMessage = true;
            rect.serverDownloading = false;
            self.serverDownloading = false;
          });
        }
      }
    });
  })
);
