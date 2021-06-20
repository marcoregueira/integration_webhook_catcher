using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ego.WebHookCatcher.Contract;

namespace Ego.WebHookCatcher.RequestStore
{
    public class HookResult
    {
        public string Id { get; }
        public CatchRequest CatchRequest { get; }
        public DateTime Creation { get; }
        public DateTime Expiration { get; set; }
        private SemaphoreSlim Semaphore { get; set; } = new SemaphoreSlim(0);

        public bool Received { get; private set; }

        public string Body { get; set; }
        public string Method { get; internal set; }
        public Dictionary<string, string[]> RequestHeaders { get; set; } = new Dictionary<string, string[]> { };
        public string ContentType { get; set; }
        public int? StatusCode { get; set; }


        public HookResult(string id, CatchRequest request)
        {
            CatchRequest = request;
            Creation = DateTime.Now;
            Id = id;
        }

        public HookResult(string id)
        {
            Creation = DateTime.Now;
            Id = id;
        }

        public async Task<HookResult> WaitAsync(CancellationToken token)
        {
            try
            {
                await Semaphore.WaitAsync(token);
            }
            catch (OperationCanceledException ex) { }

            return this;
        }

        public void ResultReceived()
        {
            Received = true;
            Semaphore.Release();
        }
    }
}
