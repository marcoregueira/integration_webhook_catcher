using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Net;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Ego.WebHookCatcher;
using EgoCatcher.Tests.Utils;
using Ego.WebHookCatcher.Contract;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace EgoCatcher.Tests
{
    public class ClientTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        public TestConfiguration Config { get; }
        public HttpClient Client { get; }

        public ClientTests(WebApplicationFactory<Startup> fixture)
        {
            Config = new TestConfiguration();
            var remoteConfig = Config.Get<RemoteTest>();
            if (!string.IsNullOrWhiteSpace(remoteConfig?.BaseAddress))
            {
                Client = new HttpClient();
                Client.BaseAddress = new Uri(remoteConfig.BaseAddress);
            }
            else
            {
                Client = fixture.CreateClient();
            }
        }

        [Fact]
        public async Task Catch_Expires_IfNotSuccessful()
        {
            // Arrange
            var id = Guid.NewGuid().ToString("N");
            var firstCall = Client.GetAsync("/catch/" + id);

            Thread.Sleep(200);
            var status = await Client.GetAsync("/catch/pending");
            var text = await status.Content.ReadAsStringAsync();
            text.Should().Contain(id, "Hook needs to be in the pending list");

            await firstCall;

            var status2 = await Client.GetAsync("/catch/pending");
            var text2 = await status2.Content.ReadAsStringAsync();
            text2.Should().NotContain(id, "Hook should have been removed from the pending list");
        }

        [Fact]
        public async Task Catch_ReturnsNotfound_IfNotSuccessful()
        {
            // Arrange
            var id = Guid.NewGuid().ToString("N");
            var firstCall = await Client.GetAsync("/catch/" + id);
            var hookResult = (await firstCall.Content.ReadAsStringAsync())
                .Deserialize<CatchResponse>();

            hookResult.HookCaptured.Should().BeFalse();
        }

        [Fact]
        public async Task Catch_ReturnsVervatimCopy()
        {
            // Arrange
            var id = Guid.NewGuid().ToString("N");

            // Act
            var firstCall = Client.GetAsync("/catch/" + id);
            Thread.Sleep(400);

            var hookResponse =
                await Client.PostAsync("/catch/hook/" + id, new FormUrlEncodedContent(new Dictionary<string, string>() { { "key1", "value1" } }));

            var response = await firstCall;
            var hookResult = (await response.Content.ReadAsStringAsync()).Deserialize<CatchResponse>();

            // Assert
            hookResponse.StatusCode.Should().Be(HttpStatusCode.OK, "The hoook call should have been accepted");
            hookResult.HookCaptured.Should().BeTrue();
            hookResult.Body.Should().Be("key1=value1");
        }
    }
}
