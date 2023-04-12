using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace dsstats.cli;

internal static class SitemapService
{
    private static Regex pageRx = new(@"^@page\s+""(.*)""");

    public static void GenerateSitemap(string pagesFolder, string outputFolder, string baseUri = "https://dsstats.pax77.org")
    {
        if (!Directory.Exists(outputFolder))
        {
            Console.WriteLine($"Output folder not found. {outputFolder}");
            return;
        }

        if (!Directory.Exists(pagesFolder)) 
        {
            Console.WriteLine($"Pages folder not found. {pagesFolder}");
            return;
        }

        List<string> pageUrls = new();

        foreach (var file in Directory.EnumerateFiles(pagesFolder, "*razor"))
        {
            pageUrls.AddRange(GetPageInfo(file));
        }

        File.WriteAllText(Path.Combine(outputFolder, "sitemap.xml"), GetSitemap(pageUrls, baseUri));

        File.WriteAllText(Path.Combine(outputFolder, "robots.txt"), GetRobots(baseUri));
    }

    private static string GetRobots(string baseUri)
    {
        return
$@"User-agent: *
Allow: /

Sitemap: {baseUri}/sitemap.xml
";
    }

    private static string GetSitemap(List<string> pageUrls, string baseUri)
    {
        string lastMod = DateTime.Today.ToString(@"yyyy-MM-dd");

        StringBuilder sb = new();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

        foreach (var url in pageUrls)
        {
            sb.AppendLine("\t<url>");
            sb.AppendLine($"\t\t<loc>{baseUri}{url}</loc>");
            sb.AppendLine($"\t\t<lastmod>{lastMod}</lastmod>");
            // sb.AppendLine("\t\t<changefreq>monthly</changefreq>");
            // sb.AppendLine("\t\t<priority>0.8</priority>");
            sb.AppendLine("\t</url>");
        }

        sb.AppendLine("</urlset>");

        return sb.ToString();
    }

    private static List<string> GetPageInfo(string file)
    {
        List<string> pageUrls = new();

        using var reader = new StreamReader(file);
        var line = reader.ReadLine();

        while (line != null && line.StartsWith("@page"))
        {
            Match match = pageRx.Match(line);
            if (match.Success)
            {
                pageUrls.Add(match.Groups[1].Value);
            }
            line = reader.ReadLine();
        }
        return pageUrls;
    }
}
