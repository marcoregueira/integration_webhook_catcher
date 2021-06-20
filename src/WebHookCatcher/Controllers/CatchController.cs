using Ego.WebHookCatcher.Contract;
using Ego.WebHookCatcher.RequestStore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace WebHookCatcher.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CatchController : ControllerBase
    {
        private readonly ILogger<CatchController> _logger;
        private readonly RequestContainer container;

        public CatchController(ILogger<CatchController> logger, RequestContainer container)
        {
            _logger = logger;
            this.container = container;
        }

        [HttpGet("{id}")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult> GetAsync([FromRoute] string id)
        {
            //If we use the get version, when the callback is received, we will simply answer with a 200 OK response.
            var response = await container.WaitForAsync(id);
            return Ok(new CatchResponse(response?.Body, response?.RequestHeaders, response?.Method));
        }

        [HttpPost("configure")]
        public async Task<ActionResult> PostAsync([FromBody] CatchRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //The post version allows a more fine grained control.
            var response = await container.WaitForAsync(request.Id, request);
            return Ok(new CatchResponse(response?.Body, response?.RequestHeaders, response?.Method));
        }

        [HttpGet("pending")]
        public ActionResult<Dictionary<string, double>> PendingRequests()
        {
            var response = container.GetPendingRequestsWithDuration();
            return Ok(response);
        }

        [HttpPost("hook/{id}")]
        public async Task<ActionResult<string>> Release([FromRoute] string id, [FromForm] Dictionary<string, string> payload)
        {
            var body = await Tools.GetRequestBody(HttpContext.Request);
            _logger.LogDebug("Recibido hook " + id);
            _logger.LogDebug("Con body:" + Environment.NewLine + body);

            var response = container.Release(id, "POST",
                Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray()),
                body);

            _logger.LogDebug("Response:" + (response?.Body ?? "OK"));

            return Ok(response?.Body ?? "OK");
        }
    }
}
