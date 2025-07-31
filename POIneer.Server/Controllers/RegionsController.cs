
using Microsoft.AspNetCore.Mvc;
//using POIneer.Server.Services;

namespace POIneer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegionsController : ControllerBase
{
    private readonly string _dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");

    [HttpGet("{region}/sqlite")]
    public IActionResult DownloadRegion(string region)
    {
        var file = Path.Combine(_dataDir, $"{region}.sqlite");
        if (!System.IO.File.Exists(file)) return NotFound();
        return PhysicalFile(file, "application/x-sqlite3", Path.GetFileName(file));
    }

    [HttpGet]
    public IActionResult GetRegions()
    {
        var jsonFile = Path.Combine(_dataDir, "regions.json");
        if (!System.IO.File.Exists(jsonFile)) return NotFound();
        var json = System.IO.File.ReadAllText(jsonFile);
        return Content(json, "application/json");
    }
}
