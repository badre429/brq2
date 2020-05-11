using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Dasync.Collections;
namespace GeoMapDownloader
{


    // GeoMapDownloader.CacheUrl
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using Dapper;
    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Configuration;

    public class SaveItem
    {
        public Tiles Tile { get; set; }
        public bool NewTile { get; set; } = true;
        public TilesData Data { get; set; }
        public bool NewData { get; set; } = true;
        public CacheUrl Cache { get; set; }
        public bool NewCache { get; set; } = true;
    }
    public class TileGrabber
    {
        public static ConcurrentStack<SaveItem> SaveStack { get; set; } = new ConcurrentStack<SaveItem>();
        public TilesDbContext Db { get; set; }
        public static DownloadStats DownloadParams { get; set; } = new DownloadStats();
        public Dictionary<int, TileProvider> Providers { get; set; } = new Dictionary<int, TileProvider>();

        public Dictionary<int, Tuple<int, int>> IdsLayerProvider { get; set; } = new Dictionary<int, Tuple<int, int>>();
        private readonly System.Net.Http.IHttpClientFactory _HttpFactory;

        public IEnumerable<TileProviderDto> GetProviders()
        {
            return this.Providers.Select(o => new TileProviderDto()
            {
                Name = o.Value.Name,
                Type = o.Value.Type,
                Id = o.Value.Id,
                Layers = o.Value.Layers.Select(k => new TileLayerDto()
                {
                    Id = k.Id + o.Value.Id,
                    Name = k.Name,
                    Type = k.Type
                }).ToArray()
            }
            );

        }
        public void SetIdsLayerProvider()
        {
            this.IdsLayerProvider = this.Providers.SelectMany(o => o.Value.Layers.Select(z => new
            {
                Id = o.Value.Id + z.Id,
                Provider = o.Value.Id,
                Layer = z.Id
            }
            )).Distinct().ToDictionary(k => k.Id, k => new Tuple<int, int>(k.Provider, k.Id));

        }

        public TileGrabber(System.Net.Http.IHttpClientFactory _httpFactory, IConfiguration configuration)
        {
            _HttpFactory = _httpFactory;
            CreateProvider("OSM", "https://a.tile.openstreetmap.org/{zoom}/{x}/{y}.png", 1200);
            CreateProvider("OSM Cycle", "https://a.tile.thunderforest.com/cycle/{zoom}/{x}/{y}@2x.png?apikey=6170aad10dfd42a38d4d8c709a536f38", 1201);
            CreateProvider("OSM Transport", "https://a.tile.thunderforest.com/transport/{zoom}/{x}/{y}@2x.png?apikey=6170aad10dfd42a38d4d8c709a536f38", 1202);
            CreateProvider("OSM Humanitaire", "https://tile-b.openstreetmap.fr/hot/{zoom}/{x}/{y}.png", 1203);
            CreateProvider("Open Topo Map", "https://b.tile.opentopomap.org/{zoom}/{x}/{y}.png", 1204);
            // h = roads only
            CreateProvider("Google Standard Roadmap", "https://www.google.com/maps/vt?lyrs=m@256&gl=fr&x={x}&y={y}&z={zoom}&hl=fr", 1300);
            CreateProvider("Google Terrain", "https://www.google.com/maps/vt?lyrs=p@256&gl=fr&x={x}&y={y}&z={zoom}&hl=fr", 1400);
            CreateProvider("Google Terrain Only", "https://www.google.com/maps/vt?lyrs=t@256&gl=fr&x={x}&y={y}&z={zoom}&hl=fr", 1500);
            CreateProvider("Google Satellite Only", "https://www.google.com/maps/vt?lyrs=s@256&gl=fr&x={x}&y={y}&z={zoom}&hl=fr", 1600);
            CreateProvider("Google Hybrid", "https://www.google.com/maps/vt?lyrs=y@256&gl=fr&x={x}&y={y}&z={zoom}&hl=fr", 1700);
            TileProvider tileProvider = CreateProvider("Mapbox", " https://api.mapbox.com/v4/mapbox.mapbox-terrain-v2,mapbox.mapbox-streets-v7/{zoom}/{x}/{y}.vector.pbf?access_token={ApiKey}", 1800, "vector", "pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA");
            tileProvider.Layers.Add(new TileLayer
            {
                FormatUrl = "",
                Name = "FontLayer",
                Type = "vector"
            });
            foreach (TileLayer layer in tileProvider.Layers)
            {
                layer.Mime = "application/x-protobuf";
            }

            CreateProvider("Esri Topo", "https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{zoom}/{y}/{x}", 2200);
            CreateProvider("Esri Streets", "https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{zoom}/{y}/{x}", 2300);

            CreateProvider("Esri Imagery/Satellite", "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{zoom}/{y}/{x}", 2400);
            CreateProvider("Yandex Imagery/Satellite", "http://sat04.maps.yandex.net/tiles?l=sat&x={x}&y={y}&z={zoom}", 2500);
            CreateProvider("MAPBOX Imagery/Satellite", "https://api.mapbox.com/v4/mapbox.satellite/{zoom}/{x}/{y}.png?access_token={ApiKey}", 3700, "raster", "pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA");

            CreateProvider("BING Imagery/Satellite", "http://ecn.dynamic.t3.tiles.virtualearth.net/comp/CompositionHandler/", 3900, "raster", "raster", (long x, long y, long zoom, string key, string culture) => BingTile((int)x, (int)y, (int)zoom, key, culture));


            SetIdsLayerProvider();
            Db = OpenDB();

        }
        public static string BingTile(int x, int y, int zoom, string key, string culture)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = zoom; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((x & mask) != 0)
                {
                    digit++;
                }
                if ((y & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            var hk = quadKey.ToString();
            return "http://ecn.dynamic.t3.tiles.virtualearth.net/comp/CompositionHandler/" + hk + "?mkt=fr-fr&it=A";


        }
        public (int provider, int layer) GetTileLayerId(int layerId = 0)
        {
            int provider = 0;
            int layer = 0;

            if (IdsLayerProvider.ContainsKey(layerId))
            {
                (provider, layer) = IdsLayerProvider[layerId];
            }
            return (provider, layer);

        }
        public async Task<(string mime, byte[] data)> GetTileByLayerId(long x, long y, long zoom, int layerId = 0)
        {
            var r = GetTileLayerId(layerId);
            return await GetTile(x, y, zoom, r.provider, r.layer);

        }
        public async Task<(string mime, byte[] data)> GetTile(long x, long y, long zoom, int provider = 0, int layer = 0)
        {
            TileProvider prv = null;
            if (Providers.ContainsKey(provider))
            {
                prv = Providers[provider];
            }
            else prv = Providers.FirstOrDefault().Value;
            var typeId = layer;
            var r = GetTileFromDb(x, y, zoom, typeId);
            byte[] retdata = null;
            if (r.data == null)
            {
                var httpdata = await prv.GetTile(x, y, zoom, layer);
                retdata = httpdata.data;
                SaveTileToDb(x, y, zoom, typeId, ref r.inofmration, ref r.data, retdata);

                while (SaveStack.Count > 10000)
                {
                    SaveToDb(null);
                    await Task.Delay(2000);
                }
            }
            else
            {
                retdata = r.data.Tile;
            }
            return (prv.GetTileMime(layer), retdata);

        }

        public TileProvider CreateProvider(string name, string url, int id, string type = "raster", string apikey = "", Func<long, long, long, string, string, string> func = null)
        {
            TileProvider tileProvider = new TileProvider(_HttpFactory);
            tileProvider.Name = name;
            tileProvider.Id = id;
            tileProvider.FormatUrl = url;
            tileProvider.Type = type;
            tileProvider.ApiKey = apikey;
            tileProvider.Type = type;
            tileProvider.CreateUrlFunction = func;
            tileProvider.InitLayer(type);
            Providers[id] = tileProvider;

            return tileProvider;
        }

        public TilesDbContext OpenDB()
        {
            if (this.Db == null)
            {
                this.Db = new TilesDbContext();
            }
            return this.Db;
        }



        private (Tiles inofmration, TilesData data) GetTileFromDb(long x, long y, long zoom, int provider = 0)
        {
            Tiles tile = FindTile(x, y, zoom, provider);
            if (tile == null)
            {
                return (null, null);
            }
            var id = tile.DataId.HasValue ? tile.DataId : tile.Id;
            TilesData data = FindTileData(id);
            return (tile, data);
        }

        private TilesData FindTileData(long? id)
        {
            TilesData result;
            using (var cs = GetConnectionString())
            {
                result = cs.QueryFirstOrDefault<TilesData>("select * from TilesData where id=" + id.ToString());
            }
            return result;
            // return this.Db.TilesData.Where(v => v.Id == id).AsNoTracking().FirstOrDefault();

        }


        private Tiles FindTile(string hash)
        {
            Tiles result;
            using (var cs = GetConnectionString())
            {
                result = cs.QueryFirstOrDefault<Tiles>($"select * from Tiles where hash='{hash}'");
            }
            return result;
            // return this.Db.Tiles.Where(v => v.Hash == hash).AsNoTracking().FirstOrDefault();
        }

        private CacheUrl FindUrlCache(string url)
        {
            CacheUrl result;
            using (var cs = GetConnectionString())
            {
                result = cs.QueryFirstOrDefault<CacheUrl>($"select * from CacheUrl where Url='{url.Replace("'", "\\'")}'");
            }
            return result;
            // return this.Db.CacheUrl.Where(((CacheUrl o) => o.Url == url)).AsNoTracking().FirstOrDefault();
        }

        private Tiles FindTile(long x, long y, long zoom, int provider)
        {

            Tiles result;
            using (var cs = GetConnectionString())
            {
                result = cs.QueryFirstOrDefault<Tiles>($"select * from Tiles where x={x} and y={y} and zoom={zoom} and Type={provider}");
            }
            return result;
            // return Db.Tiles.Where(v => v.X == x && v.Y == y && v.Zoom == zoom && v.Type == provider).AsNoTracking().FirstOrDefault();
        }
        private HashSet<long> FindTileRangeY(long x, long zoom, int provider, long minY, long maxY)
        {

            HashSet<long> result;
            using (var cs = GetConnectionString())
            {
                var query = $"select y from Tiles where y>={minY} and  y<={maxY} and x={x} and zoom={zoom} and Type={provider}";
                result = cs.Query<long>(query).ToHashSet();
            }
            return result;
            // return Db.Tiles.Where(v => v.X == x && v.Y == y && v.Zoom == zoom && v.Type == provider).AsNoTracking().FirstOrDefault();
        }
        public System.Data.Common.DbConnection GetConnectionString(string cs = null)
        {
            if (string.IsNullOrEmpty(cs))
            {
                cs = "Datasource=wwwroot/data.sqlite";
            }
            var ret = new SqliteConnection(cs);
            // ret.Open();
            return ret;
        }


        private long SaveTileToDb(long x, long y, long zoom, int provider, ref Tiles tileInfo, ref TilesData tileData, byte[] data)
        {
            var saveItem = new SaveItem();
            if (data == null || data.Length == 0)
            {
                return -1;
            }
            string hash = "";
            using (MD5 mD = MD5.Create())
            {
                hash = Convert.ToBase64String(mD.ComputeHash(data));
            }
            if (tileInfo == null)
            {
                tileInfo = new Tiles()
                {
                    X = x,
                    Y = y,
                    Zoom = zoom,
                    Type = provider,

                    CacheTime = DateTime.Now
                };
                var tileHash = FindTile(hash);
                if (tileHash != null)
                {
                    tileInfo.DataId = tileHash.Id;
                }
                else
                {
                    tileInfo.Hash = hash;
                }
                // this.Db.Tiles.Add(tileInfo);
                saveItem.Tile = tileInfo;

            }
            else
            {
                tileInfo.CacheTime = DateTime.Now;
                // tc.Update(tileInfo);
                saveItem.Tile = tileInfo;
                saveItem.NewTile = false;
            }
            if (!tileInfo.DataId.HasValue)
            {
                if (tileData == null)
                {
                    // TODO: Impletent hash data system 
                    tileData = new TilesData()
                    {

                        // Id = tileInfo.Id,
                        Tile = data
                    };
                    // this.Db.TilesData.Add(tileData);
                    saveItem.Data = tileData;

                }
                else
                {
                    tileData.Tile = data;
                    saveItem.Data = tileData;
                    saveItem.NewData = false;

                    // tdc.Update(tileData);
                }
            }
            SaveToDb(saveItem);

            return tileInfo.Id;
        }


        public void StartDownloading(RectDto rect)
        {
            DownloadParams.IsActive = true;
            DownloadParams.StartZoom = rect.StartZoom;
            DownloadParams.EndZoom = rect.EndZoom;
            DownloadParams.ProviderId = rect.ProviderId;
            DownloadParams.Points = rect.Points;
            if (IdsLayerProvider.ContainsKey(rect.ProviderId))
            {
                var prid = IdsLayerProvider[rect.ProviderId].Item1;
                DownloadParams.Name = Providers[prid].Name;
            }

            DownloadParams.StartLat = Math.Max(rect.C1.Lat, rect.C2.Lat);
            DownloadParams.EndLat = Math.Min(rect.C1.Lat, rect.C2.Lat);
            DownloadParams.StartLng = Math.Min(rect.C1.Lng, rect.C2.Lng);
            DownloadParams.EndLng = Math.Max(rect.C1.Lng, rect.C2.Lng);
            Thread thread = new Thread(new ThreadStart(async () => await this.DownloadWorker()));
            thread.Start();
        }

        public async Task<CacheUrl> GetCache(string url)
        {
            CacheUrl c2 = FindUrlCache(url);
            if (c2 != null)
            {
                return c2;
            }
            c2 = new CacheUrl
            {
                Url = url,
                Action = "get",
                Headers = new Dictionary<string, string>()
            };
            var client = _HttpFactory.CreateClient("tile");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36");
            var response = await client.GetAsync(url);

            c2.Data = await response.Content.ReadAsByteArrayAsync();
            if ((!response.IsSuccessStatusCode) || c2.Data == null || c2.Data.Length == 0)
            {
                return c2;
            }
            if (response.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string> j))
            {
                string i = j.FirstOrDefault();
                c2.Headers.Add("Content-Type", i);
            }
            else
            {
                c2.Headers.Add("Content-Type", "application/blob");
            }

            if (response.Content.Headers.TryGetValues("Content-Encoding", out IEnumerable<string> k))
            {
                string i = k.FirstOrDefault();
                c2.Headers.Add("Content-Encoding", i);
            }

            c2.Headers = c2.Headers;
            // this.Db.CacheUrl.Add(c2);
            SaveToDb(new SaveItem() { Cache = c2 });

            return c2;
        }
        public static object SaveLockObject = new object();
        public static bool SaveRunning = false;
        public void SaveToDb(SaveItem save)
        {
            if (save != null) SaveStack.Push(save);
            if (!SaveRunning)
            {
                bool runSaveTask = false;
                lock (SaveLockObject)
                {
                    if (!SaveRunning)
                    {
                        SaveRunning = true;
                        runSaveTask = true;
                    }
                }
                if (runSaveTask) Task.Run(async () =>
                 {
                     int maxItemCount = 500;
                 StartDownloading:
                     try
                     {
                         int itemCount = Math.Min(SaveStack.Count, maxItemCount);
                         if (itemCount > 50)
                         {
                             System.Console.WriteLine($"save tiles:{itemCount}");
                         }
                         var db = new TilesDbContext();
                         SaveItem[] arr = new SaveItem[itemCount];
                         if (SaveStack.TryPopRange(arr) > 0)
                         {
                             foreach (var item in arr)
                             {
                                 if (item.Cache != null)
                                 {
                                     if (item.NewCache)
                                     {
                                         db.CacheUrl.Add(item.Cache);
                                     }
                                     else
                                     {
                                         db.Entry(item.Cache).State = EntityState.Modified;
                                     }
                                 }
                                 if (item.Tile != null)
                                 {
                                     if (item.NewTile)
                                     {
                                         db.Tiles.Add(item.Tile);

                                     }
                                     else
                                     {
                                         db.Entry(item.Tile).State = EntityState.Modified;
                                     }
                                     if (item.Data != null)
                                     {
                                         if (item.NewData)
                                         {
                                             db.TilesData.Add(item.Data);
                                         }
                                         else
                                         {
                                             db.Entry(item.Data).State = EntityState.Modified;

                                         }
                                         item.Tile.TilesData = item.Data;
                                     }
                                 }
                                 else if (item.Data != null)
                                 {
                                     if (item.NewData)
                                     {
                                         db.TilesData.Add(item.Data);
                                     }
                                     else
                                     {
                                         db.Entry(item.Data).State = EntityState.Modified;

                                     }
                                 }
                             }
                             await db.SaveChangesAsync();

                         }
                     }
                     catch (Exception)
                     {

                     }

                     if (SaveStack.Count > 0)
                     {
                         goto StartDownloading;
                     }
                     lock (SaveLockObject)
                     {
                         SaveRunning = false;
                     }


                 });

            }

        }
        public async Task DownloadWorker()
        {
        global:
            long count = 0;
            if (DownloadParams.PointCount > 0)
            {
                DownloadParams.Zoom = DownloadParams.StartZoom;
                DownloadParams.StartLat = DownloadParams.Points[DownloadParams.CurrentPoint].Lat + DownloadParams.Delta;
                DownloadParams.EndLat = DownloadParams.Points[DownloadParams.CurrentPoint].Lat - DownloadParams.Delta;
                DownloadParams.StartLng = DownloadParams.Points[DownloadParams.CurrentPoint].Lng - DownloadParams.Delta;
                DownloadParams.EndLng = DownloadParams.Points[DownloadParams.CurrentPoint].Lng + DownloadParams.Delta;
            }
            for (int currentZoom = DownloadParams.StartZoom; currentZoom <= DownloadParams.EndZoom; currentZoom++)
            {
                if (DownloadParams.IsActive == false)
                {
                    return;
                }
                DownloadParams.Zoom = currentZoom;
                count = 0;
                var StartTile = GeoTileMaths.WorldToTilePos(DownloadParams.StartLng, DownloadParams.StartLat, currentZoom);
                var EndTile = GeoTileMaths.WorldToTilePos(DownloadParams.EndLng, DownloadParams.EndLat, currentZoom);
                DownloadParams.StatrtY = (long)Math.Floor(StartTile.Y);
                DownloadParams.StatrtX = (long)Math.Floor(StartTile.X);
                DownloadParams.EndY = (long)Math.Ceiling(EndTile.Y);
                DownloadParams.EndX = (long)Math.Ceiling(EndTile.X);
                var TotalX = DownloadParams.EndX - DownloadParams.StatrtX;
                var TotalY = DownloadParams.EndY - DownloadParams.StatrtY;
                DownloadParams.Count = 0;
                DownloadParams.Total = TotalX * TotalY;
                for (int x = 0; x < TotalX; x++)
                {
                    if (DownloadParams.IsActive == false)
                    {
                        return;
                    }


                    var set = FindTileRangeY(x + DownloadParams.StatrtX, currentZoom, DownloadParams.ProviderId, DownloadParams.StatrtY, DownloadParams.StatrtY + TotalY);

                    var lst = new List<long>();
                    for (int i = 0; i < TotalY; i++)
                    {
                        if (!set.Contains(i)) lst.Add(i);
                    }
                    var defCount = TotalY - lst.Count;
                    DownloadParams.Count = Math.Max(DownloadParams.Count, Interlocked.Add(ref count, defCount));
                    await lst.ParallelForEachAsync(async (y) =>
                    // var v = Parallel.For(0, TotalY, new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (y, Loop) =>
                    //  for (int y = 0; y < TotalY; y++)
                    {
                        if (DownloadParams.IsActive == false)
                        {
                            return;
                            //Loop.Stop();
                        }
                        if (!set.Contains(y + DownloadParams.StatrtY))
                        {
                            try
                            {
                                await GetTileByLayerId(x + DownloadParams.StatrtX, y + DownloadParams.StatrtY, currentZoom, DownloadParams.ProviderId);
                            }
                            catch (System.Exception)
                            {
                            };
                        }
                        // Interlocked.Add(ref count, 1);
                        DownloadParams.Count = Math.Max(DownloadParams.Count, Interlocked.Add(ref count, 1));
                    }
                    , 128
                    );
                    while (SaveStack.Count > 0)
                    {
                        SaveToDb(null);
                        await Task.Delay(2000);
                    }

                }

            }
            DownloadParams.CurrentPoint++;
            if (DownloadParams.CurrentPoint < DownloadParams.PointCount)
            {
                goto global;
            }
            lock (DownloadParams)
            {
                DownloadParams.IsActive = false;
            }
        }

    }
}
