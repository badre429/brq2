using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GeoMapDownloader
{


    // GeoMapDownloader.CacheUrl
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Http.Extensions;

    // public class HomeController : ControllerBase
    // {
    //     public ActionResult Index()
    //     {
    //         return Redirect("/index.html");
    //     }
    // }
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TileController : ControllerBase
    {
        // GET api/values
        [HttpGet("/tile/{zoom}/{x}/{y}/")]
        public async Task<ActionResult> Get([FromServices]TileGrabber tile, [FromRoute]long x, [FromRoute] long y, [FromRoute]long zoom, [FromQuery] int layerId = 0)
        {
            var r = await tile.GetTileByLayerId(x, y, zoom, layerId);
            if (r.mime == "application/x-protobuf")
            {
                this.Response.Headers.Add("Content-Encoding", "gzip");
            }
            return File(r.data, r.mime);
        }

        [HttpGet("/TileByPosition/{zoom}/{lng}/{lat}/")]
        public async Task<ActionResult> TileByPosition([FromServices]TileGrabber tile, [FromRoute]double lng, [FromRoute] double lat, [FromRoute]int zoom, [FromQuery] int layerId = 0)
        {
            (long x, long y) = GeoTileMaths.Tile(lng, lat, zoom);
            var r = await tile.GetTileByLayerId(x, y, zoom, layerId);
            return File(r.data, r.mime);
        }
        [HttpGet()]
        public ActionResult<IEnumerable<TileProviderDto>> Providers([FromServices]TileGrabber grabber)
        {

            return Ok(grabber.GetProviders().ToList());
        }

        [HttpPost()]
        public ActionResult<RectDto> StartDownloading([FromServices]TileGrabber grabber, [FromBody]RectDto rect)
        {
            lock (TileGrabber.DownloadParams)
            {
                if (TileGrabber.DownloadParams.IsActive == false)
                {
                    TileGrabber.DownloadParams.IsActive = true;
                    grabber.StartDownloading(rect);
                }
            }
            return Ok(rect);
        }

        [HttpGet()]
        public ActionResult<DownloadStats> DownloadState([FromServices]TileGrabber grabber)
        {
            return Ok(TileGrabber.DownloadParams);
        }
        [HttpGet()]
        public ActionResult<DownloadStats> DownloadCancel([FromServices]TileGrabber grabber)
        {
            lock (TileGrabber.DownloadParams)
            {
                TileGrabber.DownloadParams.IsActive = false;
            }
            return Ok(TileGrabber.DownloadParams);
        }

        [HttpGet("/cache-url/{*url}")]
        public async Task<ActionResult> CacheUrl([FromServices] TileGrabber grabber, [FromRoute] string url)
        {
            var path = HttpContext?.Request?.GetDisplayUrl();
            url = path.Substring(path.IndexOf("/cache-url/") + "/cache-url/".Length);


            CacheUrl r = await grabber.GetCache(url);
            if (r.Headers.ContainsKey("Content-Encoding"))
            {
                Response.Headers.Add("Content-Encoding", r.Headers["Content-Encoding"]);
            }
            return this.File(r.Data, r.Headers["Content-Type"]);
        }
    }
}
