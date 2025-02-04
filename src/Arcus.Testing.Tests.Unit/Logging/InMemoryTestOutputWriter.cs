using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Logging
{
    public class InMemoryTestOutputWriter : ITestOutputHelper
    {
        private readonly Collection<string> _contents = new();

        public IEnumerable<string> Contents => _contents;

        public void Write(string message)
        {
            throw new System.NotImplementedException();
        }

        public void Write(string format, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        public void WriteLine(string message)
        {
            _contents.Add(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _contents.Add(string.Format(format, args));
        }

        public string Output { get; }
    }
}