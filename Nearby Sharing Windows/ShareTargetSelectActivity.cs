using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Com.Microsoft.Connecteddevices;
using Com.Microsoft.Connecteddevices.Remotesystems;
using Com.Microsoft.Connecteddevices.Remotesystems.Commanding;
using Com.Microsoft.Connecteddevices.Remotesystems.Commanding.Nearshare;
using Google.Android.Material.BottomSheet;
using Java.Util.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ManifestPermission = Android.Manifest.Permission;
using AndroidUri = Android.Net.Uri;

namespace Nearby_Sharing_Windows
{
    [IntentFilter(new[] { Intent.ActionSend, Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "*/*", Label = "File")]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain", Label = "Url / Text")]
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.TranslucentOverlay", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ShareTargetSelectActivity : Activity
    {
        NearShareSender NearShareSender;

        ListView DeviceDiscoveryListView;
        BottomSheetBehavior BottomSheet;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_share);

            BottomSheet = BottomSheetBehavior.From(FindViewById(Resource.Id.standard_bottom_sheet));
            BottomSheet.SetBottomSheetCallback(new Layout.BottomSheetBehaviorCallback(this));
            BottomSheet.State = BottomSheetBehavior.StateHalfExpanded;

            DeviceDiscoveryListView = FindViewById<ListView>(Resource.Id.listView1);
            DeviceDiscoveryListView.ItemClick += DeviceDiscoveryListView_ItemClick;

            RequestPermissions(new[] { ManifestPermission.AccessCoarseLocation }, 0);

            InitializePlatform();
            StartWatcher();

            NearShareSender = new NearShareSender();
        }

        ConnectedDevicesPlatform Platform { get; set; }
        async void InitializePlatform()
        {
            Platform = new ConnectedDevicesPlatform(ApplicationContext);

            // Pseudo subscriptions to make api happy
            new EventListener(Platform.AccountManager.AccessTokenRequested());
            new EventListener(Platform.AccountManager.AccessTokenInvalidated());
            new EventListener(Platform.NotificationRegistrationManager.NotificationRegistrationStateChanged());

            Platform.Start();

            ConnectedDevicesAccount account = ConnectedDevicesAccount.AnonymousAccount;
            ConnectedDevicesAddAccountResult result = (await Platform.AccountManager.AddAccountAsync(account).GetAsync()) as ConnectedDevicesAddAccountResult;
        }

        RemoteSystemWatcher Watcher { get; set; }
        void StartWatcher()
        {
            List<IRemoteSystemFilter> filters = new List<IRemoteSystemFilter>();
            filters.Add(new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal));
            filters.Add(new RemoteSystemStatusTypeFilter(RemoteSystemStatusType.Any));
            filters.Add(new RemoteSystemAuthorizationKindFilter(RemoteSystemAuthorizationKind.Anonymous));

            Watcher = new RemoteSystemWatcher(filters);
            new EventListener<RemoteSystemWatcher, RemoteSystemAddedEventArgs>(Watcher.RemoteSystemAdded()).Event += OnRemoteSystemAdded;
            new EventListener<RemoteSystemWatcher, RemoteSystemRemovedEventArgs>(Watcher.RemoteSystemRemoved()).Event += OnRemoteSystemRemoved;
            Watcher.Start();
        }

        #region RemoteSystemUI
        List<RemoteSystem> RemoteSystems = new List<RemoteSystem>();
        private void OnRemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            RunOnUiThread(() =>
            {
                RemoteSystems.Add(args.RemoteSystem);
                UpdateUI();
            });
        }

        private void OnRemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            RunOnUiThread(() =>
            {
                RemoteSystems.Remove(args.RemoteSystem);
                UpdateUI();
            });
        }

        private void UpdateUI()
        {
            DeviceDiscoveryListView.Adapter = new ArrayAdapter<string>(
                this, 
                Android.Resource.Layout.SimpleListItem1, 
                RemoteSystems.Select((x) => $"{x.DisplayName} [{x.Status}]").ToArray()
            );
        }

        private void DeviceDiscoveryListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            RemoteSystem remoteSystem = RemoteSystems[e.Position];
            SendData(remoteSystem);
        }
        #endregion

        private void SendData(RemoteSystem remoteSystem)
        {
            RemoteSystemConnectionRequest connectionRequest = new RemoteSystemConnectionRequest(remoteSystem);
            if (NearShareSender.IsNearShareSupported(connectionRequest))
            {
                CancellationToken cancellationToken;
                if (Intent.Action == Intent.ActionSend)
                {
                    if (Intent.HasExtra(Intent.ExtraStream))
                    {
                        AndroidUri file = Intent.GetParcelableExtra(Intent.ExtraStream) as AndroidUri;
                        NearShareSender.SendFileAsync(
                            connectionRequest,
                            NearShareHelper.CreateNearShareFileFromContentUri(file, ApplicationContext)
                        );
                    }
                    else
                    {
                        NearShareSender.SendUriAsync(
                            connectionRequest,
                            Intent.GetStringExtra(Intent.ExtraText)
                        );
                    }
                }
                else if (Intent.Action == Intent.ActionSend)
                {
                    IList files = Intent.GetParcelableArrayListExtra(Intent.ExtraStream);

                    List<INearShareFileProvider> fileProviders = new List<INearShareFileProvider>();
                    foreach (AndroidUri file in files)
                        fileProviders.Add(NearShareHelper.CreateNearShareFileFromContentUri(file, ApplicationContext));

                    NearShareSender.SendFilesAsync(
                        connectionRequest,
                        fileProviders.ToArray()
                    );
                }
            }
        }

        public override void OnBackPressed()
            => Finish();

        public override void Finish()
        {
            try
            {
                Watcher.Stop();
                Watcher.Dispose();

                NearShareSender.Dispose();
            }
            catch { }
            base.Finish();
        }
    }
}