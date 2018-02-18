using System;
using Windows.ApplicationModel.Background;

namespace RadioIOT
{
    public sealed class StartupTask : IBackgroundTask
    {
        internal static RadioManager s_radioManager;
        private BackgroundTaskDeferral deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            if (null == s_radioManager)
            {
                s_radioManager = new RadioManager();
                var config = await InternetRadioConfig.GetDefault();
                await s_radioManager.Initialize(config);
            }
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            deferral.Complete();
        }
    }
}
