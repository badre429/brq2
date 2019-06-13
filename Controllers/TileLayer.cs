namespace GeoMapDownloader
{
    // GeoMapDownloader.CacheUrl
    public class TileLayer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } = "raster";
        public string FormatUrl { get; set; }
        public string Mime { get; set; } = "image/png";
    }
}
