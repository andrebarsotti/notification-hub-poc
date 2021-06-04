using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
await app.RunAsync();


[ApiController]
[Route("/")]
public class IndexController: ControllerBase 
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello world!");
    
}