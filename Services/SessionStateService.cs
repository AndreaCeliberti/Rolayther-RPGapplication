using Rolayther.Models.Enums;

namespace Rolayther.Services
{
    public static class SessionStateService
    {
        private static readonly Dictionary<SessionState, SessionState[]> _transitions = new()
        {
            { SessionState.Draft, new[] { SessionState.Published, SessionState.Full, SessionState.InProgress, SessionState.Completed, SessionState.Cancelled } },
            { SessionState.Published, new[] { SessionState.Draft, SessionState.Full, SessionState.InProgress, SessionState.Completed, SessionState.Cancelled } },
            { SessionState.Full,       new[] { SessionState.Draft, SessionState.Published, SessionState.InProgress, SessionState.Completed, SessionState.Cancelled } },
            { SessionState.InProgress, new[] { SessionState.Draft, SessionState.Published, SessionState.Full, SessionState.Completed, SessionState.Cancelled } },
            { SessionState.Completed,  Array.Empty<SessionState>() },
            { SessionState.Cancelled,  Array.Empty<SessionState>() }
        };

        public static bool CanTransition(SessionState from, SessionState to)
            => _transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }
}

