using System;
using System.Collections.Generic;

namespace GeoMapDownloader
{
    public partial class Tiles
    {
        public long Id { get; set; }

        public long X { get; set; }
        public long Y { get; set; }
        public long Zoom { get; set; }


        public int Type { get; set; }

        public DateTime CacheTime { get; set; }
        public string Hash { get; set; }
        public long? DataId { get; set; }

        public virtual TilesData TilesData { get; set; }
    }
}
