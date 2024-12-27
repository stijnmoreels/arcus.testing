---
sidebar_label: Getting started
---

# Getting started with Arcus Testing
**Welcome to Arcus Testing! 🎉**

This page is dedicated to be used as a walkthrough on how to integrate Arcus Testing in new and existing projects.

## The basics
The libraries in Arcus Testing are split up in these main categories:
- **Core infrastructure** (contains tech-independent functionality)
- **Assertions** (contains ways to verify functionality)
- **Logging** (contains ways to use Microsoft's `ILogger` in your test project)
- **Technology fixtures** (contains ways to interact with technology in your tests)

Depending on the context of your project, you might use one or more of these categories. The following guides will show you how these categories can be used in new or existing projects.

## Step-by-step guides
> 🎉 All classes described here are available in the same namespace (`Arcus.Testing`), regardless which library you install.

### Where does your integration tests get their values from?
Usually, integration tests projects need to have configuration values: HTTP endpoints of deployed applications, access keys to authenticate to a deployed service... In your project, these values might come in from environment variables, `appsettings.json` files, or other places.

⚡ Arcus Testing provides a `TestConfig` class that implements Microsoft's `IConfiguration`. This class already has the `appsettings.json` and optional (local) `appsetting.local.json` files embedded upon creation. Meaning that you don't have to re-create this in each test project.

1. Install the `Arcus.Testing.Core` NuGet package;
2. Locate the place where your tests retrieve their values;
3. Use the `var config = TestConfig.Create()` to create a default instance;
4. Use the common `config["Your:Config:Key]` syntax to retrieve your value.

> 🔗 See [the dedicated feature documentation](./02-Features/01-core.md) for more information on this `Arcus.Testing.Core` package and what other common test operations you repeatably use, like polling, reading local files, etc.

### Do you test if XML, JSON or CSV contents are equal?
Integration tests usually use content types like XML, JSON or CSV to pass data between systems. When asserting on whether the system used or transformed the data correctly, you have to do an 'equal' check on that data. The problem arises when elements are in a different order, have different casing or contain values that you don't care about, but are there anyway.

⚡ Arcus Testing provides several `Assert[Xml/Json/Csv].Equal` classes to make this equalization check easier for you. Fully customizable with options to ignore elements, node order, and each time with a clear assertion failure message (including line number and element names) on what part is considered 'not equal'.

1. Install the `Arcus.Testing.Assert` NuGet package;
2. Locate the places where you do an equalization check;
3. Load both the expected and actual contents as `string` (or `JsonNode`, `XmlDocument`...);
4. Use the `Assert[Xml/Json/Csv].Equal` method to check for equality.

> 🔗 See [the dedicated feature documentation](./02-Features/02-assertion.mdx) for more information on this `Arcus.Testing.Assert` package and what other equalization and failure reporting options you can use.

### Do you write log messages to the test output?
The test output is usually the first place you look when a test fails. Either the testing framework has written the exception message to the output, and assertion method has collected some failure message, or you have written some necessary context to understand (without debugging) why a test failed.

Testing frameworks all have their different ways of writing log messages to the test output, which means that each piece of test code that interacts with these test framework-specifics, is more tightly coupled to that framework.

⚡ Arcus Testing provides a way to use Microsoft's `ILogger` infrastructure in your tests instead of relying on test framework specifics. This way, you are free to write framework-independent test infrastructure.
It also helps with passing arguments to implementation code that relies on `ILogger`.

1. Install the `Arcus.Testing.Logging.[Xunit/NUnit/MSTest]` package, according to your test framework;
2. Locate the places where you pass an `ILogger` or use the test framework-dependent logger.
3. Create an `new Xunit/NUnit/MSTestTestLogger(...)` instance that takes in the framework dependent logger.
4. Now, use the `ILogger`-implemented test logger instead.

> 🔗 See [the dedicated feature documentation](.//02-Features/03-logging.mdx) for more information on these `Arcus.Testing.Logging.[Xunit/NUnit/MSTest]` packages.
