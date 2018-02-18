using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;

namespace RadioIOT
{
    public sealed class RadioManager
    {
        private IDictionary<string, string> Stations = new Dictionary<string, string>
        {
            ["101.ru - Euro Hist"] = "http://ic7.101.ru:8000/c16_13?",
            ["101.ru - Korol&Shut"] = "http://ic7.101.ru:8000/c13_31?",
            ["Dance Wave!"] = "http://stream.dancewave.online:8080/dance.mp3",
            ["QMR"] = "http://78.129.146.97:7027/stream",
            ["Minsk 92.4"] = "http://93.84.113.142:8000/radio",
            ["101.ru - Piknik"] = "http://ic7.101.ru:8000/a157? "
        };

        private IPlaybackManager _radioPlaybackManager;
        private IDevicePowerManager _radioPowerManager;
        private InternetRadioConfig _config;

        private Bot _bot;
        private uint _playbackRetries;
        private string currentUri;
        private const uint maxRetries = 3;

        public IAsyncAction Initialize(InternetRadioConfig config)
        {
            return Task.Run(async () =>
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
                if (uriToPlay == null)
                    uriToPlay = Stations["101.ru - Euro Hist"];
                currentUri = uriToPlay;
            })
            .ContinueWith((t) =>
            {
                _bot = new Bot(config.Key, Stations);
                _bot.RadioChangeRequest += RadioUriChanged;
                _bot.CommandRequest += CommandChanged;

                return _bot.Start();
            })
            .AsAsyncAction();
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
                    await _bot.SendMessage($"Volume is {_radioPlaybackManager.Volume}");
                    break;
                case BotCommand.VolumeUp:
                    _radioPlaybackManager.Volume += .1;
                    SaveSettings("volume", _radioPlaybackManager.Volume.ToString());
                    await _bot.SendMessage($"Volume is {_radioPlaybackManager.Volume}");
                    break;
            }
        }

        private async void RadioPlaybackManager_PlaybackStateChanged(object sender, PlaybackStateChangedEventArgs e)
        {
            await _bot.SendMessage($"{e.State.ToString()}");
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
                await _bot.SendMessage("Play Track failed due to null track");
                return;
            }
            await _bot.SendMessage("Play " + uri);
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