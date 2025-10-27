using System;

namespace SharedCockpitClient.Walkaround
{
    public sealed class SimWalkaround
    {
        private bool _walkaroundEnabled;
        private string _currentCamera = "cabin";

        public event Action<bool>? OnWalkaroundChanged;
        public event Action<string>? OnCameraChanged;

        public bool IsWalkaroundEnabled => _walkaroundEnabled;
        public string CurrentCamera => _currentCamera;

        public void EnableWalkaround()
        {
            if (_walkaroundEnabled)
                return;

            _walkaroundEnabled = true;
            Console.WriteLine("[Walkaround] Activado modo avatar");
            OnWalkaroundChanged?.Invoke(true);
        }

        public void DisableWalkaround()
        {
            if (!_walkaroundEnabled)
                return;

            _walkaroundEnabled = false;
            Console.WriteLine("[Walkaround] Desactivado modo avatar");
            OnWalkaroundChanged?.Invoke(false);
        }

        public void SetCamera(string camera)
        {
            if (string.IsNullOrWhiteSpace(camera))
                return;

            if (!string.Equals(camera, _currentCamera, StringComparison.OrdinalIgnoreCase))
            {
                _currentCamera = camera;
                Console.WriteLine($"[Walkaround] Cámara cambiada a {camera}");
                OnCameraChanged?.Invoke(camera);
            }
        }
    }
}
