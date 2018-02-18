using Microsoft.Maker.Media.UniversalMediaEngine;
using System;
using System.Threading.Tasks;

namespace RadioIOT
{
    internal enum PlaybackState
    {
        Error_MediaInvalid = 0,
        Error_LostConnection,

        Stopped = 100,
        Paused,
        Loading,
        Playing,
        Ended
    }

    internal struct VolumeChangedEventArgs
    {
        public double Volume;
    }

    internal struct PlaybackStateChangedEventArgs
    {
        public PlaybackState State;
    }

    delegate void VolumeChangedEventHandler(object sender, VolumeChangedEventArgs e);
    delegate void PlaybackStatusChangedEventHandler(object sender, PlaybackStateChangedEventArgs e);

    internal interface IPlaybackManager
    {
        event VolumeChangedEventHandler VolumeChanged;
        event PlaybackStatusChangedEventHandler PlaybackStateChanged;

        double Volume
        {
            get;
            set;
        }

        PlaybackState PlaybackState
        {
            get;
        }

        Task InitialzeAsync();
        void Play(Uri mediaAddress);
        void Pause();
        void Stop();
    }

    class MediaEnginePlaybackManager : IPlaybackManager
    {
        private MediaEngine _mediaEngine;
        private PlaybackState _state;

        public event VolumeChangedEventHandler VolumeChanged;
        public event PlaybackStatusChangedEventHandler PlaybackStateChanged;

        public MediaEnginePlaybackManager()
        {

        }

        public double Volume
        {
            get
            {
                return _mediaEngine.Volume;
            }
            set
            {
                if (value >= 0 && value <= 1)
                {
                    _mediaEngine.Volume = value;
                    VolumeChanged(this, new VolumeChangedEventArgs() { Volume = value });
                }
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                return _state;
            }
            internal set
            {
                if (_state != value)
                {
                    _state = value;
                    PlaybackStateChanged(this, new PlaybackStateChangedEventArgs() { State = _state });
                }
            }
        }

        private void MediaEngine_MediaStateChanged(MediaState state)
        {
            switch (state)
            {
                case MediaState.Loading:
                    PlaybackState = PlaybackState.Loading;

                    break;

                case MediaState.Stopped:
                    PlaybackState = PlaybackState.Paused;

                    break;

                case MediaState.Playing:
                    PlaybackState = PlaybackState.Playing;
                    break;

                case MediaState.Error:
                    PlaybackState = PlaybackState.Error_MediaInvalid;
                    break;

                case MediaState.Ended:
                    PlaybackState = PlaybackState.Ended;
                    break;
            }
        }

        public async Task InitialzeAsync()
        {
            _mediaEngine = new MediaEngine();
            var result = await _mediaEngine.InitializeAsync();

            _mediaEngine.MediaStateChanged += MediaEngine_MediaStateChanged;
        }

        public void Play(Uri mediaAddress)
        {
            var addressString = mediaAddress.ToString();
            _mediaEngine.Play(addressString);
        }

        public void Pause()
        {
            _mediaEngine.Pause();
            PlaybackState = PlaybackState.Paused;
        }

        public void Stop()
        {
            _mediaEngine.Stop();
            PlaybackState = PlaybackState.Stopped;
        }
    }
}
