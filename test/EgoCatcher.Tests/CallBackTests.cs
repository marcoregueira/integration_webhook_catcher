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

namespace EgoCatcher.Tests
{
    public class CallbackTests : IClassFixture<WebApplicationFactory<Startup>>
    {

        public HttpClient Client { get; }
        public TestConfiguration Config { get; }

        public CallbackTests(WebApplicationFactory<Startup> fixture)
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
        public async Task Hook_IsAccepted_UsingBodyScan()
        {
            // Hook catcher has two different working modes:
            // - The remote system calls a custom url that includes the request id in it (i.e.: http://my.shop.com/payment/a34-34556)
            // - The remote system calls always the same url (i.e.: http://my.shop.com/ipn-hook) and includes identification data in the body

            // This test emulates a situation where while we are waintin for a remote call,
            // we receive a call that includes the request id in its body

            // Arrange
            var requestId = Guid.NewGuid().ToString("N");
            var firstCall = Client.GetAsync("/catch/" + requestId);
            Thread.Sleep(400);

            // Act
            // Receive hook
            var bodyContents = new FormUrlEncodedContent(new Dictionary<string, string>() { { "key1", requestId } });
            var hookResponse = await Client.PostAsync("/unknown_hook_url/", bodyContents);

            var response = await firstCall;
            var hookResult = (await response.Content.ReadAsStringAsync()).Deserialize<CatchResponse>();


            // Assert
            hookResponse.StatusCode.Should().Be(HttpStatusCode.OK, "The hoook call should have been accepted");
            hookResult.HookCaptured.Should().BeTrue();
            hookResult.Body.Should().Contain(requestId);
        }


        [Fact]
        public async Task Hook_IsAccepted_UsingRequestString()
        {
            // Hook catcher has two different working modes:
            // - The remote system calls a custom url that includes the request id in it (i.e.: http://my.shop.com/payment/a34-34556)
            // - The remote system calls always the same url (i.e.: http://my.shop.com/ipn-hook) and includes identification data in the body

            // This test emulates a situation where while we are waintin for a remote call,
            // we receive a call that includes the request id in its URI

            // Arrange
            var requestId = Guid.NewGuid().ToString("N");
            var firstCall = Client.GetAsync("/catch/" + requestId);
            Thread.Sleep(400);

            // Act
            // Receive hook
            var bodyContents = new FormUrlEncodedContent(new Dictionary<string, string>() { { "key1", "value" } });
            var hookResponse = await Client.PostAsync($"/unknown_hook_url/{requestId}?some=thing", bodyContents);

            var response = await firstCall;
            var hookResult = (await response.Content.ReadAsStringAsync()).Deserialize<CatchResponse>();


            // Assert
            hookResponse.StatusCode.Should().Be(HttpStatusCode.OK, "The hoook call should have been accepted");
            hookResult.HookCaptured.Should().BeTrue();
            hookResult.Body.Should().Contain("key1=value");
        }

        [Fact]
        public async Task Hook_IsAccepted_And_GivesPreconfiguredResponse()
        {
            // Hook catcher has two different working modes:
            // - The remote system calls a custom url that includes the request id in it (i.e.: http://my.shop.com/payment/a34-34556)
            // - The remote system calls always the same url (i.e.: http://my.shop.com/ipn-hook) and includes identification data in the body

            // This tests uses the post request to wait for the callback 
            // and return a preconfigured response to the third party

            // Arrange
            var requestId = Guid.NewGuid().ToString("N");
            var request = JsonConvert.SerializeObject(new CatchRequest()
            {
                Id = requestId,
                MediaType = "application/json",
                ResponseBody = "{\"fake\": \"response\"}"
            });

            var payload = new StringContent(request, Encoding.UTF8, "application/json");
            var firstCall = Client.PostAsync("/catch/configure", payload);
            Thread.Sleep(400);

            // Act
            var bodyContents = new FormUrlEncodedContent(new Dictionary<string, string>() { { "key1", "value" } });
            var hookResponse = await Client.PostAsync($"/unknown_hook_url?some={requestId}", bodyContents);

            // Assert
            var response = await firstCall;
            var hookResult = (await response.Content.ReadAsStringAsync()).Deserialize<CatchResponse>();
            var forwardedResponse = (await hookResponse.Content.ReadAsStringAsync());

            hookResponse.StatusCode.Should().Be(HttpStatusCode.OK, "The hoook call should have been accepted");
            hookResult.HookCaptured.Should().BeTrue();
            hookResult.Body.Should().Contain("key1=value"); //this is the hook call payload
            forwardedResponse.Should().Contain("{\"fake\": \"response\"}"); //this is the preconfigured answer returned by the callback
        }

        [Fact]
        public async Task Hook_ReturnsNotFound_IfBodyScanFails()
        {
            // Hook catcher has two different working modes:
            // - The remote system calls a custom url that includes the request id in it (i.e.: http://my.shop.com/payment/a34-34556)
            // - The remote system calls always the same url (i.e.: http://my.shop.com/ipn-hook) and includes identification data in the body

            // This test emulates a situation where while we are waintin for a remote call,
            // we receive another unexpected call.


            // Arrange
            // wait for a remote call by id
            var requestId = Guid.NewGuid().ToString("N");
            var cancellation = new CancellationTokenSource();
            var firstCall = Client.GetAsync("/catch/" + requestId, cancellation.Token);
            Thread.Sleep(400);

            // Act
            // We receive a callback call, but doesn't include our id
            var hookResponse =
                await Client.PostAsync("/unknown_hook_url/", new FormUrlEncodedContent(new Dictionary<string, string>() { { "key1", "is_not_id" } }));

            var response = await firstCall;
            var hookResult = (await response.Content.ReadAsStringAsync()).Deserialize<CatchResponse>(); //this will wait until timeout

            // Assert
            hookResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "The hoook should be rejected");
            hookResult.HookCaptured.Should().BeFalse();
            hookResult.Body.Should().BeNull();
        }
    }
}
