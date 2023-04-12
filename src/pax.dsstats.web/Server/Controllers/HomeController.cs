using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace pax.dsstats.web.Server.Controllers;

public class HomeController : ControllerBase
{
    private readonly IMemoryCache memoryCache;

    public HomeController(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }

    [Route("/robots.txt")]
    public string Robots()
    {
        if (!memoryCache.TryGetValue("robots.txt", out string robots))
        {
            robots = System.IO.File.ReadAllText("/data/ds/robots.txt");
            memoryCache.Set("robots.txt", robots, TimeSpan.FromDays(1));
        }
        return robots;
    }

    [Route("/sitemap.xml")]
    public string Sitemap()
    {
        if (!memoryCache.TryGetValue("sitemap.xml", out string sitemap))
        {
            sitemap = System.IO.File.ReadAllText("/data/ds/sitemap.xml");
            memoryCache.Set("sitemap.xml", sitemap, TimeSpan.FromDays(1));
        }
        return sitemap;
    }
}
