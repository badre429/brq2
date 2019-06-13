namespace GeoMapDownloader
{


    // GeoMapDownloader.CacheUrl
    using System.Collections.Generic;

    public class TileProviderDto
    {
        public string Type { get; set; } = "raster";
        public string Name { get; set; }
        public int Id { get; set; }
        public IEnumerable<TileLayerDto> Layers { get; set; }
    }
}
