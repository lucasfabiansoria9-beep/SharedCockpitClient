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
            set => _current = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
