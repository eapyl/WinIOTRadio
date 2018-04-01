using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.System.Threading;

namespace RadioIOT
{
    public sealed class StartupTask : IBackgroundTask
    {
        //private static IDictionary<string, string> Stations = new Dictionary<string, string>
        //{
        //    ["101.ru - Euro Hist"] = "http://ic7.101.ru:8000/c16_13?",
        //    ["101.ru - Korol&Shut"] = "http://ic7.101.ru:8000/c13_31?",
        //    ["Dance Wave!"] = "http://stream.dancewave.online:8080/dance.mp3",
        //    ["QMR"] = "http://78.129.146.97:7027/stream",
        //    ["Minsk 92.4"] = "http://93.84.113.142:8000/radio",
        //    ["101.ru - Piknik"] = "http://ic7.101.ru:8000/a157?",
        //    ["MUZO.FM"] = "http://stream4.nadaje.com:8002/muzo",
        //};

        internal static RadioManager s_radioManager;
        internal static Bot _bot;
        private BackgroundTaskDeferral deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            var config = await InternetRadioConfig.GetDefault();

            _bot = new Bot(config.Key, config.Owner, config.RadioLink);

            await ThreadPool.RunAsync(item => _bot.Start());

            if (null == s_radioManager)
            {
                s_radioManager = new RadioManager();
                await s_radioManager.Initialize(config, _bot);
            }
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            deferral.Complete();
        }
    }
}
