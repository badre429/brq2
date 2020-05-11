using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GeoMapDownloader
{
    using System;


    // GeoMapDownloader.CacheUrl
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;

    public class TileProvider
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } = "raster";
        public string Culture { get; set; } = "";
        public Func<long, long, long, string, string, string> CreateUrlFunction { get; set; }

        public virtual List<TileLayer> Layers { get; set; }
        public virtual bool HasKey { get; set; } = false;

        public virtual string ApiKey { get; set; } = "";
        public virtual string FormatUrl { get; set; }
        protected readonly System.Net.Http.IHttpClientFactory _HttpFactory;

        public TileProvider(System.Net.Http.IHttpClientFactory _httpFactory)
        {
            _HttpFactory = _httpFactory;

        }
        public string GetTileMime(int layer = 0)
        {
            var TileLayer = Layers.FirstOrDefault(v => v.Id == layer);
            if (TileLayer == null)
            {
                TileLayer = Layers.FirstOrDefault();
            }
            return TileLayer.Mime;
        }
        public async Task<(string mime, byte[] data)> GetTile(long x, long y, long zoom, int layer = 0)
        {
            var TileLayer = Layers.FirstOrDefault(v => v.Id == layer);
            if (TileLayer == null)
            {
                TileLayer = Layers.FirstOrDefault();
            }
            return await GetTile(x, y, zoom, TileLayer);
        }
        public async Task<(string mime, byte[] data)> GetTile(long x, long y, long zoom, TileLayer layer)
        {
            string url = TileUrl(x, y, zoom, layer);

            try
            {
                return await DownloadTile(layer, url);
            }
            catch (System.Exception)
            {
                try
                {
                    return await DownloadTile(layer, url);
                }
                catch (System.Exception)
                {

                    return await DownloadTile(layer, url);

                }
            }
        }

        private async Task<(string mime, byte[] data)> DownloadTile(TileLayer layer, string url)
        {
            var client = _HttpFactory.CreateClient("tile");
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36");
            HttpResponseMessage response = null;

            response = await client.GetAsync(url);
            var ret = await response.Content.ReadAsByteArrayAsync();
            if (response.IsSuccessStatusCode != true || ret == null || ret.Length == 0)
            {
                throw new Exception("Server Error");
            }
            return (layer.Mime, ret);
        }

        protected string TileUrl(long x, long y, long zoom, TileLayer layer)
        {
            if (this.CreateUrlFunction != null)
            {
                return this.CreateUrlFunction(x, y, zoom, ApiKey, Culture);
            }
            var url = layer.FormatUrl
            .Replace("{x}", x.ToString())
            .Replace("{y}", y.ToString())
            .Replace("{zoom}", zoom.ToString());
            if (!string.IsNullOrWhiteSpace(ApiKey)) url = url.Replace("{ApiKey}", ApiKey);
            if (!string.IsNullOrWhiteSpace(Culture)) url = url.Replace("{Culture}", Culture);
            return url;
        }

        public virtual void InitLayer(string type)
        {
            this.Layers = (string.IsNullOrWhiteSpace(FormatUrl)) ? new List<TileLayer>() : new List<TileLayer>(){
                new TileLayer(){
                    FormatUrl=FormatUrl,
                    Type=type,
                    Name ="default"
                }
            };
        }


    }
}
