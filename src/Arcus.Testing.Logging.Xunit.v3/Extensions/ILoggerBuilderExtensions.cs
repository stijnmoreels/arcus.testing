﻿using System;
using Arcus.Testing;
using Xunit;

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

            return builder.AddProvider(new XunitLoggerProvider(outputWriter));
        }

        [ProviderAlias("Xunit")]
        private sealed class XunitLoggerProvider : ILoggerProvider
        {
            private readonly ILogger _logger;

            internal XunitLoggerProvider(ITestOutputHelper outputWriter)
            {
                _logger = new XunitTestLogger(outputWriter);
            }

            /// <summary>
            /// Creates a new <see cref="ILogger" /> instance.
            /// </summary>
            /// <param name="categoryName">The category name for messages produced by the logger.</param>
            /// <returns>The instance of <see cref="ILogger" /> that was created.</returns>
            public ILogger CreateLogger(string categoryName) => _logger;

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}
