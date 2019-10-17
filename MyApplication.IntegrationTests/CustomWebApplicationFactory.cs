using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MyApplication.Data;
using MyApplication.IntegrationTests.Helpers;

namespace RazorPagesProject.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Create a new service provider.
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                // Add a database context (ApplicationDbContext) using an in-memory 
                // database for testing.
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>((options, context) =>
                {
                    context.UseInMemoryDatabase("InMemoryDbForTesting")
                        .UseInternalServiceProvider(serviceProvider);
                });

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database
                // context (ApplicationDbContext).
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var logger = scopedServices
                        .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();
                    var userManager = scopedServices.GetRequiredService<UserManager<IdentityUser>>();

                    // Ensure the database is created.
                    db.Database.EnsureCreated();

                    if (!db.Database.IsInMemory())
                    {
                        throw new Exception("Database is not in-memory");
                    }

                    try
                    {
                        // Seed the database with test data.
                        var alice = new IdentityUser
                        {
                            UserName = "alice@example.org",
                            Email = "alice@example.org"
                        };
                        var result = userManager.CreateAsync(alice, "#SecurePassword123").Result;
                        if (!result.Succeeded)
                        {
                            throw new Exception("Unable to create alice:\r\n" + string.Join("\r\n", result.Errors.Select(error => $"{error.Code}: {error.Description}")));
                        }

                        var emailConfirmationToken = userManager.GenerateEmailConfirmationTokenAsync(alice).Result;
                        result = userManager.ConfirmEmailAsync(alice, emailConfirmationToken).Result;
                        if (!result.Succeeded)
                        {
                            throw new Exception("Unable to verify alices email address:\r\n" + string.Join("\r\n", result.Errors.Select(error => $"{error.Code}: {error.Description}")));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred seeding the " +
                            "database with test messages. Error: {Message}", ex.Message);
                    }
                }
            });
        }

        public async Task<HttpClient> CreateAuthenticatedClientAsync(string userName, string password)
        {
            var client = CreateClient();

            var loginPage = await HtmlHelpers.GetDocumentAsync(await client.GetAsync("/Identity/Account/Login"));

            var responseMessage = await client.SendAsync(
                (IHtmlFormElement)loginPage.QuerySelector("form[id='account']"),
                (IHtmlButtonElement)loginPage.QuerySelector("form[id='account']")
                    .QuerySelector("button"),
                new Dictionary<string, string>
                {
                    { "Input.Email", userName },
                    { "Input.Password", password },
                    { "Input.RememberMe", "false" }
                });

            return client;
        }
    }
}
