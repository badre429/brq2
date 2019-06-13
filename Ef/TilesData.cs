using System;
using System.Collections.Generic;

namespace GeoMapDownloader
{
    public partial class TilesData
    {
        public long Id { get; set; }
        public byte[] Tile { get; set; }

        public virtual IEnumerable<Tiles> Tiles { get; set; }
    }
}
