using Microsoft.Extensions.DependencyInjection;

namespace GeoMapDownloader
{
    // GeoMapDownloader.CacheUrl
    public static class TileServicesRegister
    {
        public static void AddTilesConfig(this IServiceCollection services)
        {
            services.AddSingleton(typeof(TileGrabber), typeof(TileGrabber));
        }
    }
}
