using System;
using Arcus.Testing;
using Xunit.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extensions on the <see cref="ILoggingBuilder"/> related to logging.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class ILoggerBuilderExtensions
    {
        /// <summary>
        /// Adds the logging messages from the given xUnit <paramref name="outputWriter"/> as a provider to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The logging builder to add the xUnit logging test messages to.</param>
        /// <param name="outputWriter">The xUnit test logger used across the test suite.</param>
        /// <exception cref="ArgumentNullException">Thrown when either the <paramref name="builder"/> or the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static ILoggingBuilder AddXunitTestLogging(this ILoggingBuilder builder, ITestOutputHelper outputWriter)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(outputWriter);

            var logger = new XunitTestLogger(outputWriter);
            var provider = new CustomLoggerProvider(logger);

            return builder.AddProvider(provider);
        }
    }

    /// <summary>
    /// Represents an <see cref="ILoggerProvider"/> implementation to provide <see cref="XunitTestLogger"/> instances.
    /// </summary>
    [ProviderAlias("Xunit")]
    public sealed class XunitTestLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitTestLoggerProvider" /> class.
        /// </summary>
        public XunitTestLoggerProvider(ITestOutputHelper outputWriter)
        {
            ArgumentNullException.ThrowIfNull(outputWriter);
            _outputWriter = outputWriter;
        }

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            var logger = new XunitTestLogger(_outputWriter);
            return logger;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
