using System.Net.Http;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using Xamarin.Essentials;

namespace notificationtracker
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public partial class MainActivity : AppCompatActivity
    {
        public static MainActivity Instance;

        private Button bttFilter;
        private Button bttSystemConfig;
        private Button bttUrlConfig;
        private EditText edtUrl;
        private static volatile PowerManager.WakeLock lockStatic = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Instance = this;
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            bttFilter = FindViewById<Button>(Resource.Id.bttfilterapp);
            bttSystemConfig = FindViewById<Button>(Resource.Id.bttsystemconfig);
            bttUrlConfig = FindViewById<Button>(Resource.Id.bttUrlConfig);
            edtUrl = FindViewById<EditText>(Resource.Id.urlText);

            bttFilter.Click += BttFilter_Click;
            bttSystemConfig.Click += BttSystemConfig_Click;
            bttUrlConfig.Click += BttUrlConfig_Click;

            StartSynchronization();

            GetLock(this).Acquire();
        }

        private void BttUrlConfig_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(edtUrl.Text))
                return;

            Preferences.Set(Configs.Config_Url, edtUrl.Text);
            apiEndpoint = edtUrl.Text;
        }

        private void BttSystemConfig_Click(object sender, System.EventArgs e)
        {
            Intent intent = new Intent("android.settings.ACTION_NOTIFICATION_LISTENER_SETTINGS");
            StartActivity(intent);
        }

        private void BttFilter_Click(object sender, System.EventArgs e)
        {
            StartActivity(new Intent(this, typeof(ApplicationListActivity)));
        }

        private static PowerManager.WakeLock GetLock(Context context)
        {
            PowerManager manager = (PowerManager)context.GetSystemService(Context.PowerService);
            lockStatic = manager.NewWakeLock(WakeLockFlags.Partial, "com.omnicasa.notificationtracker");
            lockStatic.SetReferenceCounted(true);
            return lockStatic;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
