using B2BSelfSignup.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.JSInterop.Implementation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace B2BSelfSignup.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IOptions<InvitationOptions> _options;

        public HomeController(
            ILogger<HomeController> logger, 
            ITokenAcquisition tokenAcquisition,
            IOptions<InvitationOptions> options)
        {
            _logger = logger;
            _tokenAcquisition = tokenAcquisition;
            _options = options;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogTrace("Home.Index starting");
            var token = await _tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default", _options.Value.HostTenantName);
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Inviting an existing user does not create a new user

            // Invite user
            var email = User.FindFirst("preferred_username").Value;
            var invitation = new
            {
                invitedUserEmailAddress = email,
                inviteRedirectUrl = _options.Value.RedirectUrl
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/invitations")
            {
                Content = new StringContent(JsonSerializer.Serialize(invitation), Encoding.UTF8, "application/json")
            };
            var resp = await http.SendAsync(request);
            if (resp.IsSuccessStatusCode)
            {
                _logger.LogInformation($"User {email} successfully invited");
                var json = await resp.Content.ReadAsStringAsync();
                var newId = JsonDocument.Parse(json).RootElement.GetProperty("invitedUser").GetProperty("id").GetString();
                var redeemUrl = JsonDocument.Parse(json).RootElement.GetProperty("inviteRedeemUrl").GetString();
                request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.microsoft.com/v1.0/groups/{_options.Value.GroupObjectId}/members/$ref")
                {
                    Content = new StringContent(
                        $"{{\"@odata.id\": \"https://graph.microsoft.com/v1.0/directoryObjects/{newId}\"}}",
                        Encoding.UTF8, "application/json")
                };
                resp = await http.SendAsync(request);
                if (resp.IsSuccessStatusCode)
                    _logger.LogInformation($"User {email} added to group");
                else if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    json = await resp.Content.ReadAsStringAsync();
                    var msg = JsonDocument.Parse(json).RootElement.GetProperty("error").GetProperty("message").GetString();
                    if (msg.StartsWith("One or more added object references already exist"))
                        _logger.LogInformation($"User {email} already exists as member of the group");
                    else
                    {
                        _logger.LogError($"Unusual bad request error when adding {email} to the security group");
                        return Error();
                    }
                }
                else
                {
                    _logger.LogError($"Failed to add {email} to the security group");
                    return Error();
                }
                Response.Redirect(redeemUrl);
            } else
            {
                Response.Redirect("/error");
            }
            _logger.LogTrace("Home.Index existing");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
