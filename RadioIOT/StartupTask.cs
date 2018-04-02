using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.System.Threading;

namespace RadioIOT
{
    public sealed class StartupTask : IBackgroundTask
    {
        internal static RadioManager s_radioManager;
        internal static Bot _bot;
        private BackgroundTaskDeferral deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            var config = await InternetRadioConfig.GetDefault();

            _bot = new Bot(config.Key, config.Owner, config.RadioLink);

            await ThreadPool.RunAsync(_ => _bot.Start());

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
