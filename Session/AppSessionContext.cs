#nullable enable
using System;

namespace SharedCockpitClient.Session
{
    public static class AppSessionContext
    {
        private static StartupSessionInfo? _current;

        public static StartupSessionInfo Current
        {
            get => _current ?? throw new InvalidOperationException("La sesión aún no fue inicializada.");
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _current = value;
            }
        }
    }
}
