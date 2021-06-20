namespace Ego.WebHookCatcher.Contract
{
    public class CatchRequest
    {
        public string Id { get; set; }
        public int StatusCode { get; set; } = 200;
        public string MediaType { get; set; }
        public string ResponseBody { get; set; }
    }
}
