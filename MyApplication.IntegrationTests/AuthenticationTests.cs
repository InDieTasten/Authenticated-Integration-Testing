using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApplication.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using MyApplication.IntegrationTests.Helpers;
using System.Linq;
using RazorPagesProject.Tests;

namespace MyApplication.IntegrationTests
{
    public class AuthenticationTests
    : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;

        public AuthenticationTests(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });
        }

        [Fact]
        public async Task Get_SecurePageRequiresAnAuthenticatedUser()
        {
            // Act
            HttpResponseMessage response = await _client.GetAsync("/Home/Secure");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("http://localhost/Identity/Account/Login",
                response.Headers.Location.OriginalString);
        }

        [Theory]
        [InlineData("alice@example.org", "#SecurePassword123")]
        public async Task Get_SecurePageWorksForAlice(string email, string password)
        {
            // Arrange
            var client = await _factory.CreateAuthenticatedClientAsync(email, password);

            // Act
            HttpResponseMessage response = await client.GetAsync("/Home/Secure");

            // Assert
            var document = await HtmlHelpers.GetDocumentAsync(response);
            Assert.Equal("THIS-IS-MY-SECRET-ONLY-VISIBLE-TO-LOGGED-IN-USERS",
                document.QuerySelector("div[class='secret']")?.TextContent);
            Assert.Equal(email,
                document.QuerySelector("div[class='user-id']")?.TextContent);
        }
    }
}
