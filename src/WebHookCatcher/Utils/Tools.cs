using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace WebHookCatcher.Controllers
{
    internal class Tools
    {
        internal static async Task<string> GetRequestBody(HttpRequest request)
        {
            if (request != null)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
                var r = new StreamReader(request.Body);
                var body = await r.ReadToEndAsync();
                return body;
            }
            return "";
        }
    }
}
