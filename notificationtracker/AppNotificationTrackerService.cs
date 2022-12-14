using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Service.Notification;
using Newtonsoft.Json;
using Xamarin.Essentials;

namespace notificationtracker
{
    [Service(
        Name = "com.omnicasa.notificationtracker.AppNotificationTrackerService",
        Exported = true,
        Label = "AppNotificationTrackerService",
        Permission = Manifest.Permission.BindNotificationListenerService
    )]
    [IntentFilter(new[] {
        "android.service.notification.NotificationListenerService"
    })]
    public class AppNotificationTrackerService : NotificationListenerService
    {
        private readonly object lockObject = new object();
        private AppConfigChangeBroadcastReceiver appConfigChangeBroadcastReceiver;
        private List<string> appSettings = new List<string>();

        public override void OnCreate()
        {
            base.OnCreate();
            AppConfigChangeBroadcastReceiver.Action = FetchApp;
            appConfigChangeBroadcastReceiver = new AppConfigChangeBroadcastReceiver();
            RegisterReceiver(appConfigChangeBroadcastReceiver, new IntentFilter(Configs.Config_AppConfigChangeBroadcastReceiver));
            FetchApp();
        }

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            if (sbn.Notification == null || string.IsNullOrEmpty(sbn.PackageName))
                return;

            if (AppConfig().Contains(sbn.PackageName))
            {
                var noti = sbn.Notification;
                var title = GetPropertySafely(noti, Notification.ExtraTitle);
                var titleBig = GetPropertySafely(noti, Notification.ExtraTitleBig);
                var text = GetPropertySafely(noti, Notification.ExtraText);
                var subText = GetPropertySafely(noti, Notification.ExtraSubText);
                var summaryText = GetPropertySafely(noti, Notification.ExtraSummaryText);
                var bigText = GetPropertySafely(noti, Notification.ExtraBigText);
                var extraInfoText = GetPropertySafely(noti, Notification.ExtraInfoText);
                var ticker = noti.TickerText?.ToString();

                var notification = new AppNotification();
                notification.Package = sbn.PackageName;
                notification.Content = new AppNotificationText()
                {
                    ExtraTitle = title,
                    ExtraTitleBig = titleBig,
                    ExtraText = text,
                    ExtraSubText = subText,
                    ExtraSummaryText = summaryText,
                    ExtraBigText = bigText,
                    ExtraInfoText = extraInfoText,
                    Ticker = ticker
                };

                Intent intent = new Intent();
                intent.SetAction(Configs.Config_NotificationReceivingBroadcastReceiver);
                intent.PutExtra(Configs.Config_AppNotification, JsonConvert.SerializeObject(notification));
                SendBroadcast(intent);
            }
        }

        private string GetPropertySafely(Notification notification, string propKey)
        {
            try
            {
                var propCharSequence = notification.Extras?.GetString(propKey);
                return $"{propCharSequence}";
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterReceiver(appConfigChangeBroadcastReceiver);
        }

        private List<string> AppConfig()
        {
            lock(lockObject)
            {
                return appSettings;
            }
        }

        private void AppConfig(List<string> items)
        {
            lock(lockObject)
            {
                appSettings = items == null ? new List<string>() : items; 
            }
        }

        private void FetchApp()
        {
            Task.Factory.StartNew(() =>
            {
                var settings = Preferences.Get(Configs.Config_AppList, string.Empty);
                if (!string.IsNullOrEmpty(settings))
                {
                    var appSet = JsonConvert.DeserializeObject<List<string>>(settings);
                    if (appSet != null & appSet.Any())
                        AppConfig(appSet);
                }
            });
        }
    }
}