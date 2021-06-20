using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ego.WebHookCatcher.RequestStore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace Ego.WebHookCatcher
{
    public class CatcherMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestContainer _container;
        private readonly ILogger<CatcherMiddleware> _logger;

        public CatcherMiddleware(RequestDelegate next, RequestContainer container, ILogger<CatcherMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            this._container = container;
            this._logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;
            var isWebApi = request.RouteValues.TryGetValue("controller", out _);

            if (!isWebApi &&
                !string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.Method, "HEAD", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.Method, "DELETE", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.Method, "TRACE", StringComparison.OrdinalIgnoreCase))
            {
                var url = string.Concat(
                        request.Scheme,
                        "://",
                        request.Host.ToUriComponent(),
                        request.PathBase.ToUriComponent(),
                        request.Path.ToUriComponent(),
                        request.QueryString.ToUriComponent());

                var body = await new StreamReader(request.Body).ReadToEndAsync() ?? "";

                _logger.LogDebug("Call captured using middleware: " + url);
                _logger.LogDebug("Body:" + Environment.NewLine + body);

                var requestKeys = _container.GetPendingRequests();
                foreach (var pendingRequest in requestKeys)
                {
                    var keyIsInUrl = url.Contains(pendingRequest);
                    var keyIsInBody = body.Contains(pendingRequest);
                    if (keyIsInUrl || keyIsInBody)
                    {
                        var response = _container.Release(pendingRequest, request.Method, headers: CopyHeadersToDictionary(request), body);
                        context.Response.StatusCode = response?.CatchRequest?.StatusCode ?? 200;
                        context.Response.ContentType = response?.CatchRequest?.MediaType ?? "application/text";
                        await context.Response.WriteAsync(response?.CatchRequest?.ResponseBody ?? "");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static Dictionary<string, string[]> CopyHeadersToDictionary(HttpRequest request)
        {
            var headers = new Dictionary<string, string[]>();
            foreach (var header in request.Headers)
            {
                headers[header.Key] = header.Value;
            }

            return headers;
        }
    }
}