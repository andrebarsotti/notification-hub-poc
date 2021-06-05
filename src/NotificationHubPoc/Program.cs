using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Configuration;

#region Application Start and Config
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();

app.MapGet("/", async http => await http.Response.WriteAsync("The app is online."));
await app.RunAsync();
#endregion

#region Configs
public class AzureNotificationHub 
{
    public string HubName { get; set; }

    public string ConnectionString { get; set;}
}
#endregion

#region Controllers

[ApiController]
[Route("[controller]")]
public class RegistroController : ControllerBase
{
  private readonly AzureNotificationHub _hubConfig;

  public RegistroController(IConfiguration configuration)
  {
      _hubConfig = configuration.GetSection(nameof(AzureNotificationHub))
                                .Get<AzureNotificationHub>();
  }
 
}

#endregion