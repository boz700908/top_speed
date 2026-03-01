using TopSpeed.Network;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void BeginManualServerEntry()
        {
            PromptServerAddressInput(_settings.LastServerAddress);
        }

        public void BeginServerPortEntry()
        {
            var current = _settings.DefaultServerPort.ToString();
            _promptTextInput(
                "Enter the default server port used for manual connections.",
                current,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    HandleServerPortInput(result.Text);
                });
        }

        private MultiplayerSession? SessionOrNull()
        {
            return _getSession();
        }
    }
}
