using Ego.WebHookCatcher.Config;
using Ego.WebHookCatcher.Contract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ego.WebHookCatcher.RequestStore
{
    public class RequestContainer
    {
        private readonly ILogger logger;
        private readonly IOptions<RequestCatchConfig> catchConfig;

        private ConcurrentDictionary<string, HookResult> PendingRequestsKeys { get; set; } = new ConcurrentDictionary<string, HookResult> { };

        public RequestContainer(ILogger<RequestContainer> logger, IOptions<RequestCatchConfig> catchConfig)
        {
            this.logger = logger;
            this.catchConfig = catchConfig;
        }

        public List<string> GetPendingRequests()
        {
            var keys = PendingRequestsKeys.Select(x => x.Key).ToList();
            return keys;
        }

        public Dictionary<string, double> GetPendingRequestsWithDuration()
        {
            var now = DateTime.Now;
            var keys = PendingRequestsKeys.ToDictionary(x => x.Key, x => (x.Value.Creation - now).TotalSeconds);
            return keys;
        }

        public async Task<HookResult> WaitForAsync(string requestId, CatchRequest request = null)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;
            source.CancelAfter(catchConfig.Value.ExpirationSeconds * 1000);

            var result = PendingRequestsKeys.GetOrAdd(requestId, (x) => new HookResult(requestId, request));
            await result.WaitAsync(token);
            PendingRequestsKeys.TryRemove(requestId, out _);
            return result;
        }

        public HookResult Release(string key, string method, Dictionary<string, string[]> headers, string body)
        {
            PendingRequestsKeys.TryGetValue(key, out var value);
            if (value != null)
            {
                value.Method = method;
                value.RequestHeaders = headers;
                value.Body = body;
                value.ResultReceived();
                logger.LogDebug($"Received key {key} with method {method}");
            }
            return value;
        }
    }
}