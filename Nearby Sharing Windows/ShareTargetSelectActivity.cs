#nullable enable

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using Com.Microsoft.Connecteddevices;
using Com.Microsoft.Connecteddevices.Remotesystems;
using Com.Microsoft.Connecteddevices.Remotesystems.Commanding;
using Com.Microsoft.Connecteddevices.Remotesystems.Commanding.Nearshare;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.ProgressIndicator;
using Java.Util.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AndroidUri = Android.Net.Uri;
using ManifestPermission = Android.Manifest.Permission;

namespace Nearby_Sharing_Windows
{
    [IntentFilter(new[] { Intent.ActionSend, Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "*/*", Label = "File")]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain", Label = "Url / Text")]
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.TranslucentOverlay", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ShareTargetSelectActivity : AppCompatActivity
    {
        NearShareSender NearShareSender;

        ListView DeviceDiscoveryListView;
        BottomSheetBehavior BottomSheet;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_share);

            BottomSheet = BottomSheetBehavior.From(FindViewById(Resource.Id.standard_bottom_sheet));
            BottomSheet.AddBottomSheetCallback(new Layout.BottomSheetBehaviorCallback(this));
            BottomSheet.State = BottomSheetBehavior.StateHalfExpanded;

            DeviceDiscoveryListView = FindViewById<ListView>(Resource.Id.listView1)!;
            DeviceDiscoveryListView.ItemClick += DeviceDiscoveryListView_ItemClick;

            RequestPermissions();
            InitializePlatform();

            NearShareSender = new NearShareSender();
        }

        void RequestPermissions()
        {
            RequestPermissions(new[] {
                ManifestPermission.AccessFineLocation,
                ManifestPermission.AccessCoarseLocation
            }, 0);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            RunOnUiThread(() =>
            {
                StartWatcher();
            });
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
            await Platform.AccountManager.AddAccountAsync(account).GetAsync();
        }

        RemoteSystemWatcher Watcher { get; set; }
        void StartWatcher()
        {
            System.Diagnostics.Debug.Assert(Watcher == null, "Watcher already has been started!");

            List<IRemoteSystemFilter> filters = new List<IRemoteSystemFilter>();
            //filters.Add(new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal));
            filters.Add(new RemoteSystemStatusTypeFilter(RemoteSystemStatusType.Any));
            filters.Add(new RemoteSystemAuthorizationKindFilter(RemoteSystemAuthorizationKind.Anonymous));

            Watcher = new RemoteSystemWatcher(filters);
            new EventListener<RemoteSystemWatcher, RemoteSystemAddedEventArgs>(Watcher.RemoteSystemAdded()).Event += OnRemoteSystemAdded;
            new EventListener<RemoteSystemWatcher, RemoteSystemUpdatedEventArgs>(Watcher.RemoteSystemUpdated()).Event += OnRemoteSystemUpdated;
            new EventListener<RemoteSystemWatcher, RemoteSystemRemovedEventArgs>(Watcher.RemoteSystemRemoved()).Event += OnRemoteSystemRemoved;
            new EventListener<RemoteSystemWatcher, RemoteSystemWatcherErrorOccurredEventArgs>(Watcher.ErrorOccurred()).Event += OnWatcherErrorOccurred;
            Watcher.Start();
        }

        private void OnWatcherErrorOccurred(RemoteSystemWatcher sender, RemoteSystemWatcherErrorOccurredEventArgs args)
        {
            string msg = args.Error.Name();
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

        private void OnRemoteSystemUpdated(RemoteSystemWatcher sender, RemoteSystemUpdatedEventArgs args)
        {
            RunOnUiThread(() =>
            {
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

        private async void SendData(RemoteSystem remoteSystem)
        {
            RemoteSystemConnectionRequest connectionRequest = new RemoteSystemConnectionRequest(remoteSystem);
            if (NearShareSender.IsNearShareSupported(connectionRequest))
            {
                AsyncOperationWithProgress? fileTransferOperation = null;
                AsyncOperation? uriTransferOperation = null;
                if (Intent?.Action == Intent.ActionSend)
                {
                    if (Intent.HasExtra(Intent.ExtraStream))
                    {
                        AndroidUri file = (Intent.GetParcelableExtra(Intent.ExtraStream) as AndroidUri)!;
                        fileTransferOperation = NearShareSender.SendFileAsync(
                            connectionRequest,
                            NearShareHelper.CreateNearShareFileFromContentUri(file, ApplicationContext)
                        );
                    }
                    else
                    {
                        uriTransferOperation = NearShareSender.SendUriAsync(
                            connectionRequest,
                            Intent.GetStringExtra(Intent.ExtraText)
                        );
                    }
                }
                else if (Intent?.Action == Intent.ActionSend)
                {
                    IList files = (Intent.GetParcelableArrayListExtra(Intent.ExtraStream))!;

                    List<INearShareFileProvider> fileProviders = new List<INearShareFileProvider>();
                    foreach (AndroidUri file in files)
                        fileProviders.Add(NearShareHelper.CreateNearShareFileFromContentUri(file, ApplicationContext));

                    fileTransferOperation = NearShareSender.SendFilesAsync(
                        connectionRequest,
                        fileProviders.ToArray()
                    );
                }

                FindViewById<ListView>(Resource.Id.listView1)!.Visibility = Android.Views.ViewStates.Gone;
                FindViewById<LinearLayout>(Resource.Id.progressUILayout)!.Visibility = Android.Views.ViewStates.Visible;

                NearShareStatus result;
                LinearProgressIndicator progressIndicator = FindViewById<LinearProgressIndicator>(Resource.Id.sendProgressIndicator)!;
                if (fileTransferOperation != null)
                {
                    new EventListener<AsyncOperationWithProgress, NearShareProgress>(fileTransferOperation.Progress()).Event += (AsyncOperationWithProgress sender, NearShareProgress args) =>
                    {
                        RunOnUiThread(() =>
                        {
#if !DEBUG
                            try
                            {
#endif
                            progressIndicator.Max = (int)args.TotalBytesToSend;
                            progressIndicator.Progress = (int)args.BytesSent;
#if !DEBUG
                            }
                            catch { }
#endif
                        });
                    };
                    result = (await fileTransferOperation.GetAsync() as NearShareStatus)!;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(uriTransferOperation != null, "\"uriTransferOperation\" is null!");

                    // ToDo: progressIndicator.Indeterminate = true;
                    result = (await uriTransferOperation!.GetAsync() as NearShareStatus)!;
                }

                this.Finish();
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