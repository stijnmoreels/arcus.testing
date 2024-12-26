using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    /// <summary>
    /// Represents the available <see cref="EventId"/>s used throughout the test fixtures, available for custom use.
    /// </summary>
    public static class TestLogEvents
    {
        /// <summary>
        /// Gets the event ID that collects all logs related to setting up test fixtures.
        /// </summary>
        public static EventId Setup => new(9001, "Test:Setup");

        /// <summary>
        /// Gets the event ID that collects all logs related to tearing down test fixtures.
        /// </summary>
        public static EventId Teardown => new(9002, "Test:Teardown");
    }
}
