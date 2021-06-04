using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

#region Application Start and Config
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
await app.RunAsync();
#endregion

[ApiController]
[Route("/")]
public class IndexController: ControllerBase 
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello world!");
    
}