using Microsoft.Extensions.Options;

namespace Rolayther.Services.Mail
{
    // Freno al "reinvia email di conferma": l'endpoint è pubblico (chi non ha ancora fatto login deve poterlo usare),
    // quindi senza limiti si potrebbe usare per inondare di email la casella di qualcuno.
    // Per ogni indirizzo: una pausa minima tra due invii e un massimo di invii all'ora. In memoria: si azzera
    // al riavvio, ma insieme al limite per IP (policy "ResendPolicy") basta a rendere l'abuso inutile.
    public sealed class ResendThrottle
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, List<DateTime>> _sent = new();
        private readonly TimeSpan _cooldown;
        private readonly int _maxPerHour;
        private readonly Func<DateTime> _clock;

        public ResendThrottle(IOptions<EmailOptions> options)
            : this(TimeSpan.FromSeconds(options.Value.ResendCooldownSeconds), options.Value.ResendMaxPerHour, () => DateTime.UtcNow)
        {
        }

        internal ResendThrottle(TimeSpan cooldown, int maxPerHour, Func<DateTime> clock)
        {
            _cooldown = cooldown;
            _maxPerHour = Math.Max(1, maxPerHour);
            _clock = clock;
        }

        public int CooldownSeconds => (int)_cooldown.TotalSeconds;

        // true = si può inviare (e l'invio viene registrato); false = troppo presto / troppi invii
        public bool TryAcquire(string email)
        {
            var key = (email ?? string.Empty).Trim().ToLowerInvariant();
            var now = _clock();
            var hourAgo = now.AddHours(-1);

            lock (_lock)
            {
                if (_sent.Count > 5000)
                    Prune(hourAgo);

                if (!_sent.TryGetValue(key, out var times))
                {
                    times = new List<DateTime>();
                    _sent[key] = times;
                }

                times.RemoveAll(t => t < hourAgo);

                if (times.Count >= _maxPerHour)
                    return false;

                if (times.Count > 0 && now - times[^1] < _cooldown)
                    return false;

                times.Add(now);
                return true;
            }
        }

        private void Prune(DateTime hourAgo)
        {
            foreach (var key in _sent.Where(kv => kv.Value.Count == 0 || kv.Value[^1] < hourAgo).Select(kv => kv.Key).ToList())
                _sent.Remove(key);
        }
    }
}
