
using System;

using Android.App;
using Android.Content;

namespace notificationtracker
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new[] { Configs.Config_NotificationReceivingBroadcastReceiver })]
    public class NotificationReceivingBroadcastReceiver : BroadcastReceiver
    {
        public static Action<Intent> Action { set; get; }

        public override void OnReceive(Context context, Intent intent)
        {
            Action?.Invoke(intent);
        }
    }
}