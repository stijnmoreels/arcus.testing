﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Core.Fixture;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;
using static Arcus.Testing.ResourceDirectory;

namespace Arcus.Testing.Tests.Integration.Core
{
    public class TestConfigTests : IAsyncLifetime
    {
        private const string DefaultAppSettingsName = "appsettings.json",
                             DefaultLocalAppSettingsName = "appsettings.local.json";

        private static readonly Faker Bogus = new();
        private readonly DisposableCollection _disposables = new(NullLogger.Instance);

        public static IEnumerable<TheoryDataRow<Func<Action<TestConfigOptions>, TestConfig>>> CustomConfigs => new[]
        {
            new TheoryDataRow<Func<Action<TestConfigOptions>, TestConfig>>(TestConfig.Create),
            new TheoryDataRow<Func<Action<TestConfigOptions>, TestConfig>>(configureOptions => new CustomTestConfig(configureOptions)),
        };

        [Theory]
        [MemberData(nameof(CustomConfigs))]
        public void CreateCustom_WithLocalAppSettingsOnCustomMainFile_RetrievesValue(Func<Action<TestConfigOptions>, TestConfig> createConfig)
        {
            // Arrange
            string mainAppSettingsName = $"{Bogus.Lorem.Word()}.json";
            string localAppSettingsName1 = $"{Bogus.Lorem.Word()}.local.json";
            string localAppSettingsName2 = $"{Bogus.Lorem.Word()}.local.json";
            string key1 = Bogus.Random.Guid().ToString(), expected1 = Bogus.Lorem.Word();
            string key2 = Bogus.Random.Guid().ToString(), expected2 = Bogus.Lorem.Word();
            AddLocalValueToCustomMain(localAppSettingsName1, key1, expected1, mainAppSettingsName);
            AddLocalValueToCustomMain(localAppSettingsName2, key2, expected2, mainAppSettingsName);

            var config = createConfig(options =>
            {
                options.UseMainJsonFile(mainAppSettingsName)
                       .AddOptionalJsonFile(localAppSettingsName1)
                       .AddOptionalJsonFile(localAppSettingsName2);
            });

            // Act / Assert
            Assert.Equal(expected1, config[key1]);
            Assert.Equal(expected2, config[key2]);
        }

        private void AddLocalValueToCustomMain(string fileName, string key, string value, string newMainFile)
        {
            _disposables.Add(TemporaryFile.CreateAt(
                CurrentDirectory.Path,
                fileName,
                Encoding.UTF8.GetBytes($"{{ \"{key}\": \"{value}\" }}")));

            AddTokenToCustomMain(key, newMainFile);
        }

        [Theory]
        [MemberData(nameof(CustomConfigs))]
        public void CreateCustom_WithLocalAppSettingsFile_RetrievesValue(Func<Action<TestConfigOptions>, TestConfig> createConfig)
        {
            // Arrange
            string localAppSettingsName = $"{Bogus.Lorem.Word()}.json";
            string key = Bogus.Lorem.Word(), expected = Bogus.Lorem.Word();
            AddLocalValueToDefaultMain(localAppSettingsName, key, expected);

            TestConfig config = createConfig(options => options.AddOptionalJsonFile(localAppSettingsName));

            // Act
            string actual = config[key];

            // Assert
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> DefaultConfigs => new[]
        {
            new object[] { (Func<TestConfig>)(TestConfig.Create) },
            new object[] { (Func<TestConfig>)(() => new CustomTestConfig()) }
        };

        [Theory]
        [MemberData(nameof(DefaultConfigs))]
        public void CreateDefault_WithDefaultLocalValue_RetrievesValue(Func<TestConfig> createConfig)
        {
            // Arrange
            string key = Bogus.Lorem.Word(), expected = Bogus.Lorem.Word();
            AddLocalValueToDefaultMain(DefaultLocalAppSettingsName, key, expected);
            TestConfig config = createConfig();

            // Act
            string actual = config[key];

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Get_WithoutValue_FailsWithNotFound()
        {
            // Arrange
            string key = Bogus.Lorem.Word();
            AddLocalValueToDefaultMain(DefaultLocalAppSettingsName, key, value: "   ");

            string newMainFile = Bogus.Lorem.Word() + ".json";
            AddLocalValueToCustomMain(DefaultLocalAppSettingsName, key, value: "", newMainFile);

            // Act / Assert
            Assert.All(GenerateTestConfigs(uponNewMainFile: newMainFile), config =>
            {
                AssertNotFound(() => { var _ = config[key]; }, key, "non-blank", "value");
            });
        }

        private void AddLocalValueToDefaultMain(string fileName, string key, string value)
        {
            _disposables.Add(TemporaryFile.CreateAt(
                CurrentDirectory.Path,
                fileName,
                Encoding.UTF8.GetBytes($"{{ \"{key}\": \"{value}\" }}")));

            AddTokenToDefaultMain(key);
        }

        [Fact]
        public void Get_WithStillTokenInValue_FailsWithNotFound()
        {
            // Arrange
            string key = Bogus.Lorem.Word();
            AddTokenToDefaultMain(key);

            string newMainFile = Bogus.Lorem.Word() + ".json";
            AddTokenToCustomMain(key, newMainFile);

            // Act / Assert
            Assert.All(GenerateTestConfigs(uponNewMainFile: newMainFile), config =>
            {
                AssertNotFound(() => { var _ = config[key]; }, key, "still", "token");
            });
        }

        private void AddTokenToCustomMain(string key, string newMainFile)
        {
            _disposables.Add(TemporaryFile.CreateAt(CurrentDirectory.Path, newMainFile, "{ }"u8.ToArray()));
            AddTokenToDefaultMain(key, newMainFile);
        }

        private void AddTokenToDefaultMain(string key, string mainFile = DefaultAppSettingsName)
        {
            var defaultPath = new FileInfo(Path.Combine(CurrentDirectory.Path.FullName, mainFile));
            _disposables.Add(TemporaryFileEdit.At(defaultPath,
                json =>
                {
                    var jObject = JObject.Parse(json);
                    jObject[key] = "#{Token}#";

                    return jObject.ToString();
                }));
        }

        [Fact]
        public void Get_WithoutKnownKey_FailsWithNotFound()
        {
            string unknownKey = Bogus.Lorem.Word();
            Assert.All(GenerateTestConfigs(), config =>
            {
                AssertNotFound(() => { var _ = config[unknownKey]; }, unknownKey);
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        public static void Get_WithoutKey_FailsWithNotFound(string key)
        {
            Assert.All(GenerateTestConfigs(), config => AssertNotFound(() => { var _ = config[key]; }));
        }

        private static TestConfig[] GenerateTestConfigs(string uponNewMainFile = null)
        {
            var configs = new[]
            {
                TestConfig.Create(),
                TestConfig.Create(opt => opt.AddOptionalJsonFile(Bogus.Random.Word())),
                new CustomTestConfig(),
                new CustomTestConfig(opt => opt.AddOptionalJsonFile(Bogus.Random.Word()))
            };

            if (uponNewMainFile != null)
            {
                return configs.Append(TestConfig.Create(opt => opt.UseMainJsonFile(uponNewMainFile))).ToArray();
            }

            return configs;
        }

        private static void AssertNotFound(Action testCode, params string[] errorParts)
        {
            var exception = Assert.Throws<KeyNotFoundException>(testCode);
            Assert.Contains("test configuration", exception.Message);
            Assert.Contains("please make sure", exception.Message);
            Assert.All(errorParts, part => Assert.Contains(part, exception.Message));
        }

        [Fact]
        public void CreateCustom_WithoutMainAppSettingsFile_StillSucceeds()
        {
            // Arrange
            var config = TestConfig.Create(opt => opt.UseMainJsonFile(Bogus.System.FileName("json")));

            // Act / Assert
            AssertNotFound(() => { string _ = config["ignored_key"]; });
        }

        [Fact]
        public void Create_Default_Succeeds()
        {
            TestConfig.Create();
        }

        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await _disposables.DisposeAsync();
        }
    }
}
