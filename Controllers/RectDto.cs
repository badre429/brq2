using System.Collections.Generic;

namespace GeoMapDownloader
{

    // GeoMapDownloader.CacheUrl
    public class RectDto
    {
        public double? DaltaLng { get; set; }
        public double? DaltaLat { get; set; }

        public PointDto[] Points { get; set; }
        public int StartZoom { get; set; }
        public int EndZoom { get; set; }
        public int ProviderId { get; set; }
        public PointDto C1 { get; set; }
        public PointDto C2 { get; set; }
    }
}
