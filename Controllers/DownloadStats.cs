namespace GeoMapDownloader
{
    // GeoMapDownloader.CacheUrl
    public class DownloadStats
    {
        public int Zoom { get; set; }
        public bool IsActive { get; set; } = false;
        public long Count { get; set; }
        public long Total { get; set; }
        public long StatrtX { get; set; }
        public long EndX { get; set; }
        public long StatrtY { get; set; }
        public long EndY { get; set; }
        public double StartLng { get; set; }
        public double EndLng { get; set; }
        public double StartLat { get; set; }
        public double EndLat { get; set; }
        public int StartZoom { get; set; }
        public int EndZoom { get; set; }
        public int ProviderId { get; set; }
        public string Name { get; set; }
    }
}
