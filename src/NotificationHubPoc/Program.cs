using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;

#region Application Start and Config
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();

app.MapGet("/", async http => await http.Response.WriteAsync("The app is online."));
await app.RunAsync();
#endregion

[ApiController]
[Route("[controller]")]
public class RegistroController : ControllerBase
{

  public RegistroController()
  {
  }
 
}