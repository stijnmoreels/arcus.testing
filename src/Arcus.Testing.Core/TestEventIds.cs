using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available event IDs that the <see cref="ILogger"/> test implementations will use to format its test log output.
    /// </summary>
    public static class TestEventIds
    {
        /// <summary>
        /// Gets the event ID collecting logs related to setting up test infrastructure.
        /// </summary>
        public static readonly EventId OnSetup = new(1500, "[Test:OnSetup]");

        /// <summary>
        /// Gets the event ID collecting logs related to tearing down test infrastructure.
        /// </summary>
        public static readonly EventId OnTeardown = new(2000, "[Test:OnTeardown]");
    }
}
