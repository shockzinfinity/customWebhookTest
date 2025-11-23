using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;

namespace WebhookClient.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<string>> Echo()
    {
        HttpContext.Request.EnableBuffering();

        using var reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        HttpContext.Request.Body.Position = 0;

        Console.WriteLine(body);

        return Ok();
    }
}
