using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Text;
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

#region Application Start and Config
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication",
                                                                                    null);

builder.Services.AddSingleton<INotificationService, NotificationService>();

var app = builder.Build();
app.MapControllers();

app.MapGet("/", async http => await http.Response.WriteAsync("The app is online."));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

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

        if (!VerifyUserAndPwd(username, password))
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

  public NotificationService(IConfiguration configuration,
                            ILogger<NotificationService> logger)
  {
    logger.LogInformation("Obtendo configurações...");
    var hubConfig = configuration.GetSection(nameof(AzureNotificationHub))
                                 .Get<AzureNotificationHub>();
    logger.LogInformation($"Hub name => {hubConfig.HubName}");
    logger.LogInformation($"Hub conSt => {hubConfig.ConnectionString}");
    _hub = NotificationHubClient.CreateClientFromConnectionString(hubConfig.ConnectionString,
                                                                  hubConfig.HubName);
  }

  public INotificationHubClient Hub => _hub;
}
#endregion

#region Model
public class DeviceRegistration
{
  public string Platform { get; set; }
  public string Handle { get; set; }
  public string[] Tags { get; set; }
}
#endregion

#region Controllers
[Authorize]
[ApiController]
[Route("[controller]")]
public class RegistrationController: ControllerBase
{
  private readonly INotificationHubClient hub;

  public RegistrationController(INotificationService service)
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
  public async Task<IActionResult> CreatesOrUpdatesARgistrationAsync(string id, [FromBody] DeviceRegistration deviceUpdate)
  {
    RegistrationDescription registration = null;
    switch (deviceUpdate.Platform)
    {
      case "mpns":
        registration = new MpnsRegistrationDescription(deviceUpdate.Handle);
        break;
      case "wns":
        registration = new WindowsRegistrationDescription(deviceUpdate.Handle);
        break;
      case "apns":
        registration = new AppleRegistrationDescription(deviceUpdate.Handle);
        break;
      case "fcm":
        registration = new FcmRegistrationDescription(deviceUpdate.Handle);
        break;
      default:
        return BadRequest();
    }

    registration.RegistrationId = id;
    var username = User.Identity.Name;

    // add check if user is allowed to add these tags
    registration.Tags = new HashSet<string>(deviceUpdate.Tags);
    registration.Tags.Add("username:" + username);

    try
    {
      await hub.CreateOrUpdateRegistrationAsync(registration);
    }
    catch (MessagingException e)
    {
      ReturnGoneIfHubResponseIsGone(e);
    }

    return Ok();
  }

  [HttpDelete]
  public async Task<IActionResult> DeleteAsync(string id)
  {
    await hub.DeleteRegistrationAsync(id);
    return Ok();
  }

  private static void ReturnGoneIfHubResponseIsGone(MessagingException e)
  {
    var webex = e.InnerException as WebException;
    if (webex.Status == WebExceptionStatus.ProtocolError)
    {
      var response = (HttpWebResponse)webex.Response;
      if (response.StatusCode == HttpStatusCode.Gone)
        throw new HttpRequestException(HttpStatusCode.Gone.ToString());
    }
  }

}

[Authorize]
[ApiController]
[Route("[controller]")]
public class NotificationsController: ControllerBase
{
  private readonly INotificationService _service;

  public NotificationsController(INotificationService service)
  {
    _service = service;
  }

  [HttpPost]
  public async Task<IActionResult> Post(string pns, [FromBody]string message, string to_tag)
  {
    var user = User.Identity.Name;
    string[] userTag = new string[2];
    userTag[0] = "username:" + to_tag;
    userTag[1] = "from:" + user;

    Microsoft.Azure.NotificationHubs.NotificationOutcome outcome = null;
    HttpStatusCode ret = HttpStatusCode.InternalServerError;

    switch (pns.ToLower())
    {
        case "wns":
            // Windows 8.1 / Windows Phone 8.1
            var toast = @"<toast><visual><binding template=""ToastText01""><text id=""1"">" + 
                        "From " + user + ": " + message + "</text></binding></visual></toast>";
            outcome = await _service.Hub.SendWindowsNativeNotificationAsync(toast, userTag);
            break;
        case "apns":
            // iOS
            var alert = "{\"aps\":{\"alert\":\"" + "From " + user + ": " + message + "\"}}";
            outcome = await _service.Hub.SendAppleNativeNotificationAsync(alert, userTag);
            break;
        case "fcm":
            // Android
            var notif = "{ \"data\" : {\"message\":\"" + "From " + user + ": " + message + "\"}}";
            outcome = await _service.Hub.SendFcmNativeNotificationAsync(notif, userTag);
            break;
    }

    if (outcome != null)
    {
        if (!((outcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Abandoned) ||
            (outcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Unknown)))
        {
            ret = HttpStatusCode.OK;
        }
    }

    return StatusCode((int)ret);
  }  
}

#endregion