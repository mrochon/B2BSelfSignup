using B2BSelfSignup.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        public HomeController(ILogger<HomeController> logger, ITokenAcquisition tokenAcquisition)
        {
            _logger = logger;
            _tokenAcquisition = tokenAcquisition;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogTrace("Home.Index starting");
            var token = await _tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default", "meraridom.com");
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Does the user already exist? Maybe I don't need to

            // Invite user
            var email = User.FindFirst("preferred_username").Value;
            var invitation = new
            {
                invitedUserEmailAddress = email,
                inviteRedirectUrl = "https://microsoft.com"
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
                //var newId = JsonDocument.Parse(json).RootElement.GetProperty("invitedUser").GetProperty("id").GetString();
                var redeemUrl = JsonDocument.Parse(json).RootElement.GetProperty("inviteRedeemUrl").GetString();
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
