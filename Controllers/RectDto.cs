namespace GeoMapDownloader
{
    // GeoMapDownloader.CacheUrl
    public class RectDto
    {
        public int StartZoom { get; set; }
        public int EndZoom { get; set; }
        public int ProviderId { get; set; }
        public PointDto C1 { get; set; }
        public PointDto C2 { get; set; }
    }
}
