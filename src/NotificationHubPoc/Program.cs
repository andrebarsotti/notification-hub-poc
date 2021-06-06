using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net.Http.Headers;

#region Application Start and Config
var builder = WebApplication.CreateBuilder(args);

// configure basic authentication 
builder.Services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication",
                                                                                    null);

builder.Services.AddSingleton<INotificationService, NotificationService>();

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

    public string ConnectionString { get; set; }
}
#endregion

#region Authentication Handlers
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // skip authentication if endpoint has [AllowAnonymous] attribute
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

          var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
          var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
          var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
          var username = credentials[0];
          var password = credentials[1];

        if (VerifyUserAndPwd(username, password))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

        var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username),
            };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool VerifyUserAndPwd(string user, string password)
    {
        // This is not a real authentication scheme.
        return user == password;
    }
}
#endregion

#region Services
public interface INotificationService 
{
  INotificationHubClient Hub { get; }
}

public class NotificationService : INotificationService
{
  private readonly INotificationHubClient _hub;

  public NotificationService(IConfiguration configuration)
  {
    var hubConfig = configuration.GetSection(nameof(AzureNotificationHub))
                                 .Get<AzureNotificationHub>();
    _hub = NotificationHubClient.CreateClientFromConnectionString(hubConfig.ConnectionString,
                                                                  hubConfig.HubName);
  }

  public INotificationHubClient Hub => _hub;
}
#endregion

#region Controllers
[Authorize]
[ApiController]
[Route("[controller]")]
public class RegistroController : ControllerBase
{
  private readonly INotificationHubClient hub;

  public RegistroController(INotificationService service)
  {
      hub = service.Hub;
  }

  [HttpPost]
  public async Task<IActionResult> CreateRegistrationIdAsync(string handle = null)
  {
    string newRegistrationId = null;

    // make sure there are no existing registrations for this push handle (used for iOS and Android)
    if (handle != null)
    {
        var registrations = await hub.GetRegistrationsByChannelAsync(handle, 100);

        foreach (RegistrationDescription registration in registrations)
        {
            if (newRegistrationId == null)
            {
                newRegistrationId = registration.RegistrationId;
            }
            else
            {
                await hub.DeleteRegistrationAsync(registration);
            }
        }
    }

    if (newRegistrationId == null) 
        newRegistrationId = await hub.CreateRegistrationIdAsync();

    return Ok(newRegistrationId);
  }

  [HttpPut]
  public Task<IActionResult> PutAsync()
  {
    throw new NotImplementedException();
  }

  [HttpDelete]
  public Task<IActionResult> DeleteAsync()
  {
    throw new NotImplementedException();
  }

}

#endregion