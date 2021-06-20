using System;
using System.Collections.Generic;

namespace Ego.WebHookCatcher.Contract
{
    public class CatchResponse
    {
        public string Body { get; }
        public Dictionary<string, string[]> RequestHeaders { get; }
        public string Method { get; }
        public bool HookCaptured { get; set; }

        private CatchResponse() { }

        public CatchResponse(string body, Dictionary<string, string[]> requestHeaders, string method)
        {
            Body = body;
            RequestHeaders = requestHeaders;
            Method = method;
            HookCaptured = (requestHeaders?.Count ?? 0) > 1;
        }

        public override bool Equals(object obj)
        {
            return obj is CatchResponse other &&
                   Body == other.Body &&
                   EqualityComparer<Dictionary<string, string[]>>.Default.Equals(RequestHeaders, other.RequestHeaders) &&
                   Method == other.Method;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Body, RequestHeaders, Method);
        }
    }
}
