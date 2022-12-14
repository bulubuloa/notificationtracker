using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using Android.Content;
using Android.Widget;
using Newtonsoft.Json;
using Xamarin.Android.Net;
using Xamarin.Essentials;

namespace notificationtracker
{
    public partial class MainActivity
    {
        private readonly object lockObject = new object();
        private BehaviorSubject<bool> pushing = new BehaviorSubject<bool>(false);
        private BehaviorSubject<object> pushServer = new BehaviorSubject<object>(null);
        private List<AppNotification> appNotifications = new List<AppNotification>();
        private string apiEndpoint = string.Empty;

        private Timer autoRaiseSingalSync;
        private int TIME_RECHECK_SYNCUP = 5; //seconds
        private CompositeDisposable disposabless = new CompositeDisposable();

        private NotificationReceivingBroadcastReceiver notificationReceivingBroadcastReceiver;

        private void StartSynchronization()
        {
            NotificationReceivingBroadcastReceiver.Action = ReceivedNotification;
            notificationReceivingBroadcastReceiver = new NotificationReceivingBroadcastReceiver();
            RegisterReceiver(notificationReceivingBroadcastReceiver, new IntentFilter(Configs.Config_NotificationReceivingBroadcastReceiver));

            var currentUrl = Preferences.Get(Configs.Config_Url, string.Empty);
            if(!string.IsNullOrEmpty(currentUrl))
            {
                apiEndpoint = currentUrl;
            }
            else
            {
                apiEndpoint = "https://lamsim.net/apis/banking-tranfer/callback-test";
            }
            edtUrl.Text = apiEndpoint;

            autoRaiseSingalSync = new Timer(AutoRaiseSyncs_Tick, null, TimeSpan.FromSeconds(TIME_RECHECK_SYNCUP), TimeSpan.FromSeconds(TIME_RECHECK_SYNCUP));

            var observableValid = pushServer
                .Where(item => item != null)
                .Where(item => !pushing.Value)
                .Select(item => GetAppNotifications())
                .Where(items => items != null && items.Any())
                .Publish()
                .RefCount();

            var observablePush = observableValid
                .SelectMany(items => PushToServer(items))
                .Catch((Exception ex) => Observable.Return(new List<string>()))
                .Publish()
                .RefCount();

            var observableCleanNotification = observablePush
                .SelectMany(result => ClearNotification(result))
                .Catch((Exception ex) => Observable.Return(new List<AppNotification>()))
                .Publish()
                .RefCount();

            var observableState = Observable.Merge(
                observableValid.Select(x => true),
                observablePush.Select(x => true),
                observableCleanNotification.Select(x => false)
                )
                .Do(state => Console.WriteLine($"running ==> {state}"))
                .Publish()
                .RefCount();

            RegisterDisposables(new CompositeDisposable()
            {
                observableState.Subscribe(state =>
                {
                    pushing.OnNext(state);
                }),

                observableCleanNotification.Subscribe(result =>
                {
                    //pushServer.OnNext(new object());
                })
            });
        }

        public void StopSynchronization()
        {
            autoRaiseSingalSync.Dispose();
            UnregisterReceiver(notificationReceivingBroadcastReceiver);
            disposabless?.Dispose();
            disposabless = null;
        }

        private void ReceivedNotification(Intent intent)
        {
            string notification = intent.GetStringExtra(Configs.Config_AppNotification) ?? string.Empty;
            if (string.IsNullOrEmpty(notification))
                return;

            var noti = JsonConvert.DeserializeObject<AppNotification>(notification);
            if (noti != null)
            {
                Toast.MakeText(this, $"{noti.Package}: {noti.Content}", ToastLength.Short).Show();
                AddAppNotification(noti);
                pushServer.OnNext(new object());
            }
        }

        private void AutoRaiseSyncs_Tick(object sender)
        {
            if (pushing.Value || !GetAppNotifications().Any())
                return;

            pushServer.OnNext(new object());
        }

        protected void RegisterDisposables(params IDisposable[] disposables)
        {
            try
            {

                if (disposabless == null)
                    disposabless = new CompositeDisposable();
                foreach (var item in disposables.Where(c => c != null))
                {
                    disposabless.Add(new CompositeDisposable()
                    {
                        item
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        private List<AppNotification> GetAppNotifications()
        {
            lock(lockObject)
            {
                return appNotifications;
            }
        }

        private void AddAppNotification(AppNotification appNotification)
        {
            lock (lockObject)
            {
                appNotifications.Add(appNotification);
            }
        }

        private void CleanAppNotifications(IEnumerable<string> pushedItems)
        {
            lock (lockObject)
            {
                var waitingItems = appNotifications.Where(x => !pushedItems.Contains(x.Guuid));
                appNotifications = new List<AppNotification>(waitingItems);
            }
        }

        private IObservable<IEnumerable<string>> PushToServer(IEnumerable<AppNotification> appNotifications)
        {
            var observable = Observable.Create<IEnumerable<string>>(async o =>
            {
                var tokenSource = new CancellationTokenSource();
                try
                {
                    using (var httpClient = new HttpClient(new AndroidClientHandler()))
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

                        tokenSource.Token.ThrowIfCancellationRequested();
                        var parameters = JsonConvert.SerializeObject(appNotifications);
                        Console.WriteLine($"PushToServer: {parameters}");

                        using (var httpResponse = await httpClient.PostAsync(
                            new Uri(apiEndpoint),
                            new StringContent(parameters, Encoding.UTF8, "application/json"),
                            tokenSource.Token).ConfigureAwait(false))
                        {
                            tokenSource.Token.ThrowIfCancellationRequested();

                            if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                //Console.WriteLine("notificationtracker", $"{httpResponse.StatusCode}: {parameters}");
                                Console.WriteLine($"PushToServer: OK");
                                o.OnNext(appNotifications.Select(x => x.Guuid));
                                o.OnCompleted();
                            }
                            else
                            {
                                Console.WriteLine($"PushToServer: Failse");
                                o.OnError(new Exception("WebAPI Error"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PushToServer: Exception {ex.StackTrace}");
                    Console.WriteLine(ex.GetType().FullName);
                    o.OnError(ex);
                }
                return Disposable.Create(() =>
                {
                    tokenSource?.Cancel();
                    tokenSource?.Dispose();
                });
            });

            return observable
                .Retry(2)
                .Catch((Exception ex) =>
                {
                    return Observable.Return(new List<string>());
                });
        }

        private IObservable<IEnumerable<AppNotification>> ClearNotification(IEnumerable<string> appNotifications)
        {
            var observable = Observable.Create<IEnumerable<AppNotification>>(o =>
            {
                try
                {
                    CleanAppNotifications(appNotifications);
                    o.OnNext(new List<AppNotification>());
                    o.OnCompleted();
                }
                catch (Exception ex)
                {
                    o.OnError(ex);
                }
                return Disposable.Create(() =>
                {
                });
            });

            return observable
                .Retry(2)
                .Catch((Exception ex) =>
                {
                    return Observable.Return<IEnumerable<AppNotification>>(null);
                });
        }
    }
}
