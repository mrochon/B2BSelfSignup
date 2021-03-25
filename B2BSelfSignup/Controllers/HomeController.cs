using B2BSelfSignup.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
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
        private readonly IOptions<InvitationOptions> _invitationOptions;
        private readonly IOptions<IEnumerable<string>> _validTenants;

        public HomeController(
            ILogger<HomeController> logger,
            ITokenAcquisition tokenAcquisition,
            IOptions<InvitationOptions> invitationOptions)
        {
            _logger = logger;
            _tokenAcquisition = tokenAcquisition;
            _invitationOptions = invitationOptions;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel();
            _logger.LogTrace($"{model.CorrelationId}: Home.Index starting");
            var email = User.FindFirst("preferred_username").Value;
            model.Email = email;
            _logger.LogTrace($"{model.CorrelationId}: {email}");
            var tid = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            model.Tid = tid;
            if (tid == _invitationOptions.Value.HostTenantId)
            {
                _logger.LogTrace($"{model.CorrelationId}: Current memeber: {tid}");
                model.RedirectUrl = _invitationOptions.Value.RedirectUrl;
                model.Message = "You are already a member of this domain";
                return View(model);
            }
            if (!_invitationOptions.Value.ValidTenants.Contains(tid))
            {
                _logger.LogError($"{model.CorrelationId}: Unauthorized: {tid}");
                model.Message = $"Unauthorized {tid}.";
                return View(model);
            }

            var token = await _tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default", _invitationOptions.Value.HostTenantName);
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var invitation = new
            {
                invitedUserEmailAddress = email,
                inviteRedirectUrl = _invitationOptions.Value.RedirectUrl
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/invitations")
            {
                Content = new StringContent(JsonSerializer.Serialize(invitation), Encoding.UTF8, "application/json")
            };
            var resp = await http.SendAsync(request);
            var newId = String.Empty;
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                if (err.Contains("The invited user already exists in the directory"))
                {
                    var msg = JsonDocument.Parse(err).RootElement.GetProperty("error").GetProperty("message").GetString();
                    _logger.LogWarning($"{model.CorrelationId}: {msg}");
                    newId = msg.Split(": ")[1].Split('.')[0];
                }
                else
                {
                    _logger.LogError($"{model.CorrelationId}: Failed to process invitation. {err}");
                    model.Message = "Invitation failed.";
                    return View(model);
                }
            }
            else
            {
                _logger.LogInformation($"{model.CorrelationId}:User {email} successfully invited");
                var json = await resp.Content.ReadAsStringAsync();
                newId = JsonDocument.Parse(json).RootElement.GetProperty("invitedUser").GetProperty("id").GetString();
            }
            request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.microsoft.com/v1.0/groups/{_invitationOptions.Value.GroupObjectId}/members/$ref")
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
                var json = await resp.Content.ReadAsStringAsync();
                var msg = JsonDocument.Parse(json).RootElement.GetProperty("error").GetProperty("message").GetString();
                if (msg.StartsWith("One or more added object references already exist"))
                    _logger.LogInformation($"{model.CorrelationId}: User {email} already exists as member of the group");
                else
                {
                    _logger.LogError($"{model.CorrelationId}: Unusual bad request error when adding {email} to the security group. {msg}");
                    model.Message = "Error adding to security group.";
                    return View(model);
                }
            }
            else
            {
                var err = await resp.Content.ReadAsStringAsync();
                _logger.LogError($"{model.CorrelationId}: Failed to add {email} to the security group. {err}");
                model.Message = "Unexpected error occurred.";
                return View(model);
            }
            model.RedirectUrl = _invitationOptions.Value.RedirectUrl;
            _logger.LogTrace($"{model.CorrelationId}: Home.Index exiting");
            return View(model);
        }
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var err = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            if (Request.Query.ContainsKey("msg"))
            {
                var msg = Request.Query["msg"][0];
                err.Message = Base64UrlEncoder.Decode(msg);
            }
            return View(err);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
