using System;

namespace LibriaDbSync
{
    internal class LibriaDependencyException : Exception
    {
        public LibriaDependencyException() : base("Unable to sync the DB. Response received contains invalid data. Use Traces to locate the response.") { }
    }
}
