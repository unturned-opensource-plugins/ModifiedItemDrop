using System;
using System.Collections.Generic;
using System.Linq;

namespace FFEmqo.ModifiedItemDrop.Domain
{
    public sealed class MidCommandRouteResult
    {
        public MidCommandRouteResult(bool accepted, MidCommandRouteKind kind, string message, IEnumerable<string> arguments)
        {
            Accepted = accepted;
            Kind = kind;
            Message = message ?? string.Empty;
            Arguments = (arguments ?? Array.Empty<string>()).ToList().AsReadOnly();
        }

        public bool Accepted { get; }

        public MidCommandRouteKind Kind { get; }

        public string Message { get; }

        public IReadOnlyList<string> Arguments { get; }
    }
}
