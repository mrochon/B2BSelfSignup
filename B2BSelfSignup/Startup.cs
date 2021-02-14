using B2BSelfSignup.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2BSelfSignup
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // Use https://login.microsoftonline.com/fbgpg.com/v2.0/.well-known/openid-configuration to discover ids of tenantId

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<InvitationOptions>(options => Configuration.GetSection("Invitation").Bind(options));
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();
            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var currDelegate = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async (ctx) =>
                {
                    List<string> validTenants = new List<string>();
                    var tenants = Configuration.GetValue<string>("ValidTenantsString");
                    if (string.IsNullOrEmpty(tenants))
                    {
                        Configuration.Bind("ValidTenants", validTenants);
                    }
                    else
                    {
                        validTenants = tenants.Split(' ', ',').ToList();
                    }
                    var tid = ctx.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                    if (!validTenants.Contains(tid))
                    {
                        ctx.Fail(new Exception("Unauthorized"));
                    }
                    if (currDelegate != null)
                        await currDelegate.Invoke(ctx);
                };
                options.Events.OnRemoteFailure = async (ctx) =>
                {
                    var url = ctx.Request.GetEncodedUrl().Replace("signin-oidc", "home/error");
                    ctx.Response.Redirect($"{url}?msg={Base64UrlEncoder.Encode(ctx.Failure.Message)}");
                    ctx.HandleResponse(); // Suppress the exception
                    await Task.CompletedTask;
                };
            });

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });
            services.AddRazorPages()
                 .AddMicrosoftIdentityUI();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
