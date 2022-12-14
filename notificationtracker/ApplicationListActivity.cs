
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using Xamarin.Essentials;

namespace notificationtracker
{
    [Activity(Label = "ApplicationListActivity")]
    public class ApplicationListActivity : Activity
    {
        private ListView listView;
        private List<ApplicationModel> applications;
        private ApplicationListAdapter applicationListAdapter;

        private string google_default_sms_app = "com.google.android.apps.messaging";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_application_list);

            listView = FindViewById<ListView>(Resource.Id.listView);
            applications = new List<ApplicationModel>();
            applicationListAdapter = new ApplicationListAdapter(this, applications);
            listView.Adapter = applicationListAdapter;

            listView.ItemClick += ListView_ItemClick;

            FetchApplication();
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var item = applications[e.Position];
            item.IsSelected = !item.IsSelected;
            applicationListAdapter.NotifyDataSetChanged();

            SaveSetting();
        }

        private void FetchApplication()
        {
            Task.Factory.StartNew(() =>
            {
                var settings = Preferences.Get(Configs.Config_AppList, string.Empty);
                var appSetted = new List<string>();
                if(!string.IsNullOrEmpty(settings))
                {
                    var appSet = JsonConvert.DeserializeObject<List<string>>(settings);
                    if (appSet != null & appSet.Any())
                        appSetted = new List<string>(appSet);
                }

                var result = new List<ApplicationModel>();
                var mainIntent = new Intent(Intent.ActionMain, null);
                mainIntent.AddCategory(Intent.CategoryLauncher);

                var infos1 = PackageManager.GetInstalledPackages(Android.Content.PM.PackageInfoFlags.Activities);
                foreach (var item in infos1)
                {
                    result.Add(new ApplicationModel()
                    {
                        AppName = item.ApplicationInfo.LoadLabel(PackageManager)?.ToString(),
                        AppPackage = item.ApplicationInfo.PackageName,
                        IsSelected = appSetted.Contains(item.ApplicationInfo.PackageName)
                    });

                }

                if(result.Where(x => x.AppPackage.Contains(google_default_sms_app))?.Any() == false)
                {
                    result.Add(new ApplicationModel()
                    {
                        AppName = "GoogleDefaultMessage",
                        AppPackage = google_default_sms_app,
                        IsSelected = appSetted.Contains(google_default_sms_app)
                    });
                }

                var sorted = result.OrderBy(x => x.AppName);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    applications.AddRange(sorted);
                    applicationListAdapter.NotifyDataSetChanged();
                });
            });
        }

        private void SaveSetting()
        {
            var setted = applications.Where(x => x.IsSelected).Select(x => x.AppPackage);
            var currentApp = JsonConvert.SerializeObject(setted);
            Preferences.Set(Configs.Config_AppList, currentApp);

            Intent intent = new Intent();
            intent.SetAction(Configs.Config_AppConfigChangeBroadcastReceiver);
            SendBroadcast(intent);
        }
    }

    public class ApplicationModel
    {
        public string AppName { set; get; }
        public string AppPackage { set; get; }
        public bool IsSelected { set; get; }
    }

    public class ApplicationListAdapter : ArrayAdapter<ApplicationModel>
    {
        private List<ApplicationModel> items;
        private Context context;

        public ApplicationListAdapter(Context context, List<ApplicationModel> items) : base(context, Resource.Layout.item_application)
        {
            this.items = items;
            this.context = context;
        }

        public override int Count => items == null ? 0 : items.Count;


        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView;

            if (view == null)
            {
                view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_application, parent, false);
            }

            var photo = view.FindViewById<ImageView>(Resource.Id.icon);
            var title = view.FindViewById<TextView>(Resource.Id.title);
            var subtitle = view.FindViewById<TextView>(Resource.Id.subtitle);
            var mainlayout = view.FindViewById<LinearLayout>(Resource.Id.mainlayout);

            var item = items[position];
            title.Text = item.AppName;
            subtitle.Text = item.AppPackage;
            mainlayout.SetBackgroundColor(item.IsSelected ? Color.DarkRed : Color.White);

            return view;
        }
    }
}