
using System;

using Android.App;
using Android.Content;

namespace notificationtracker
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { Configs.Config_AppConfigChangeBroadcastReceiver })]
    public class AppConfigChangeBroadcastReceiver : BroadcastReceiver
    {
        public static Action Action { set; get; }

        public override void OnReceive(Context context, Intent intent)
        {
            Action?.Invoke();
        }
    }
}