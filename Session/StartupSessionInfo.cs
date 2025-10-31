using System;

namespace SharedCockpitClient.Session
{
    /// <summary>
    /// Información recopilada desde el RoleDialog para iniciar la sesión.
    /// </summary>
    public sealed class StartupSessionInfo
    {
        public StartupSessionInfo(string playerName, SessionRole role, string roomName, bool isPublicBroadcast, string? hostEndpoint)
        {
            PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
            Role = role;
            RoomName = roomName ?? throw new ArgumentNullException(nameof(roomName));
            IsPublicBroadcast = isPublicBroadcast;
            HostEndpoint = hostEndpoint;
        }

        public string PlayerName { get; }

        public SessionRole Role { get; }

        public string RoomName { get; }

        public bool IsPublicBroadcast { get; }

        public string? HostEndpoint { get; }
    }
}
