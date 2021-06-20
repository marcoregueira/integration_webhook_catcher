using Newtonsoft.Json;

namespace EgoCatcher.Tests.Utils
{
    public static class Extensions
    {
        public static T Deserialize<T>(this string serial)
        {
            return JsonConvert.DeserializeObject<T>(serial);
        }
    }
}
