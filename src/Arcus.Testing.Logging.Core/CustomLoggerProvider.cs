﻿using System;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    /// <summary>
    /// Custom <see cref="ILoggerProvider"/> that creates a custom <see cref="ILogger"/> provider.
    /// </summary>
#pragma warning disable S1133
    [Obsolete("Will be removed in v2.0, use the specific logging packages (Xunit, NUnit, MSTest) instead")]
#pragma warning restore
    public class CustomLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomLoggerProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger instance to return on <see cref="CreateLogger"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public CustomLoggerProvider(ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs specific application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
