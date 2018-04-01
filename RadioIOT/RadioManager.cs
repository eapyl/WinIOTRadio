using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;

namespace RadioIOT
{
    public sealed class RadioManager
    {
        private IPlaybackManager _radioPlaybackManager;
        private IDevicePowerManager _radioPowerManager;
        private InternetRadioConfig _config;

        private Bot _bot;
        private uint _playbackRetries;
        private string currentUri;
        private const uint maxRetries = 3;

        public IAsyncAction Initialize(InternetRadioConfig config, Bot bot)
        {
            _bot = bot;
            bot.RadioChangeRequest += RadioUriChanged;
            bot.CommandRequest += CommandChanged;

            var radioTask = Task.Run(async () =>
            {
                _config = config;
                _playbackRetries = 0;

                _radioPowerManager = new RadioPowerManager();
                _radioPowerManager.PowerStateChanged += RadioPowerManager_PowerStateChanged;

                _radioPlaybackManager = new MediaEnginePlaybackManager();
                _radioPlaybackManager.VolumeChanged += RadioPlaybackManager_VolumeChanged;
                _radioPlaybackManager.PlaybackStateChanged += RadioPlaybackManager_PlaybackStateChanged;
                await _radioPlaybackManager.InitialzeAsync();

                // Manage settings
                var savedVolume = LoadSettings("volume");
                if (savedVolume == null)
                    savedVolume = ".1";
                _radioPlaybackManager.Volume = Convert.ToDouble(savedVolume);

                // Wake up the radio
                _radioPowerManager.PowerState = PowerState.Powered;

                var uriToPlay = LoadSettings("play");
                if (uriToPlay != null)
                    currentUri = uriToPlay;
            });

            return radioTask.AsAsyncAction();
        }

        private async void RadioPowerManager_PowerStateChanged(object sender, PowerStateChangedEventArgs e)
        {
            switch (e.PowerState)
            {
                case PowerState.Powered:

                    await Task.Delay(_config.Delay);
                    if (null != currentUri)
                    {
                        await playChannel(currentUri);
                    }
                    break;
                case PowerState.Standby:
                    _radioPlaybackManager.Pause();
                    await Task.Delay(_config.Delay);
                    break;
            }
        }

        private async void RadioUriChanged(object sender, RadioChangeRequestEventArgs e)
        {
            var result = Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri uri);
            if (result)
            {
                await playChannel(e.Uri);
                SaveSettings("play", e.Uri);
            }
        }

        private async void CommandChanged(object sender, CommandRequestEventArgs e)
        {
            switch(e.Command)
            {
                case BotCommand.Pause:
                    _radioPlaybackManager.Pause();
                    break;
                case BotCommand.Stop:
                    _radioPlaybackManager.Stop();
                    break;
                case BotCommand.Start:
                    await playChannel(currentUri);
                    break;
                case BotCommand.VolumeDown:
                    _radioPlaybackManager.Volume -= .1;
                    SaveSettings("volume", _radioPlaybackManager.Volume.ToString());
                    _bot.SendMessage($"Volume is {_radioPlaybackManager.Volume}");
                    break;
                case BotCommand.VolumeUp:
                    _radioPlaybackManager.Volume += .1;
                    SaveSettings("volume", _radioPlaybackManager.Volume.ToString());
                    _bot.SendMessage($"Volume is {_radioPlaybackManager.Volume}");
                    break;
            }
        }

        private async void RadioPlaybackManager_PlaybackStateChanged(object sender, PlaybackStateChangedEventArgs e)
        {
            _bot.SendMessage(e.State.ToString());
            switch (e.State)
            {
                case PlaybackState.Playing:
                    _playbackRetries = 0;
                    break;
                case PlaybackState.Ended:
                    if (maxRetries > _playbackRetries)
                    {
                        await playChannel(currentUri);
                    }
                    break;
            }
        }

        private void RadioPlaybackManager_VolumeChanged(object sender, VolumeChangedEventArgs e) => SaveSettings("volume", e.Volume.ToString());

        private async Task playChannel(string uri)
        {
            if (null == uri)
            {
                _bot.SendMessage("Play Track failed due to null track");
                return;
            }
            _bot.SendMessage("Play " + uri);
            _radioPlaybackManager.Play(new Uri(uri));
        }

        private void SaveSettings(string name, string value)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[name] = value;
        }

        private string LoadSettings(string name)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(name))
            {
                return localSettings.Values[name].ToString();
            }
            return null;
        }
    }
}