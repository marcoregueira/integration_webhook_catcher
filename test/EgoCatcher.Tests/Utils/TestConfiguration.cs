using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace EgoCatcher.Tests.Utils
{
    public class TestConfiguration
    {
        public IConfigurationRoot Config { get; }

        public TestConfiguration()
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("testconfig.json")
                .AddJsonFile(GetEnvironmentSettingsFileName("testconfig.*.json"), optional: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<TestConfiguration>()
                .Build();
        }

        public T Get<T>()
        {
            return Config.GetSection(typeof(T).Name).Get<T>();
        }

        private static string GetEnvironmentSettingsFileName(string mask)
        {
            //https://stackoverflow.com/a/58745379/2982757
            var settingsFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), mask);
            if (settingsFiles.Length > 1) throw new Exception($"Expect to have at most one configuration-specfic settings file, but found {string.Join(", ", settingsFiles)}.");
            var settingsFile = settingsFiles.FirstOrDefault();
            return settingsFile ?? Guid.NewGuid().ToString();
        }
    }
}
