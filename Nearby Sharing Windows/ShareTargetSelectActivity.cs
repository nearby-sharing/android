#nullable enable

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Com.Microsoft.Connecteddevices;
using Com.Microsoft.Connecteddevices.Remotesystems;
using Com.Microsoft.Connecteddevices.Remotesystems.Commanding;
using Com.Microsoft.Connecteddevices.Remotesystems.Commanding.Nearshare;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using Java.Util.Concurrent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AndroidUri = Android.Net.Uri;
using ManifestPermission = Android.Manifest.Permission;

namespace Nearby_Sharing_Windows
{
    [IntentFilter(new[] { Intent.ActionSend, Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "*/*", Label = "Share file")]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain", Label = "Share url")]
    [Activity(Label = "@string/app_name", Exported = true, Theme = "@style/AppTheme.TranslucentOverlay", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ShareTargetSelectActivity : AppCompatActivity, View.IOnApplyWindowInsetsListener
    {
        NearShareSender NearShareSender;

        [AllowNull] ListView DeviceDiscoveryListView;
        [AllowNull] TextView StatusTextView;
        [AllowNull] FrameLayout bottomSheetFrame;
        [AllowNull] Button cancelButton;
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_share);

            StatusTextView = FindViewById<TextView>(Resource.Id.statusTextView)!;
            DeviceDiscoveryListView = FindViewById<ListView>(Resource.Id.listView1)!;
            bottomSheetFrame = FindViewById<FrameLayout>(Resource.Id.standard_bottom_sheet)!;
            cancelButton = FindViewById<Button>(Resource.Id.cancel_button)!;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                Window!.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
                Window!.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightNavigationBar;
                Window!.DecorView.SetOnApplyWindowInsetsListener(this);
            }

            DeviceDiscoveryListView.ItemClick += DeviceDiscoveryListView_ItemClick;

            RequestPermissions(new[] {
                ManifestPermission.AccessFineLocation,
                ManifestPermission.AccessCoarseLocation,
                ManifestPermission.BluetoothScan,
                ManifestPermission.BluetoothConnect
            }, 0);
            InitializePlatform();

            NearShareSender = new NearShareSender();
        }

        #region UI
        public WindowInsets? OnApplyWindowInsets(View? v, WindowInsets? windowInsets)
        {
            if (windowInsets != null)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    var insets = windowInsets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
                    bottomSheetFrame.SetPadding(
                        insets.Left,
                        /* insets.Top */ 0,
                        insets.Right,
                        insets.Bottom
                    );
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    bottomSheetFrame.SetPadding(
                        windowInsets.StableInsetLeft,
                        /* insets.Top */ 0,
                        windowInsets.StableInsetRight,
                        windowInsets.StableInsetBottom
                    );
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
            return windowInsets;
        }
        #endregion

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            if (grantResults.Contains(Android.Content.PM.Permission.Denied))
                Snackbar.Make(Window!.DecorView, "Error: Missing permission!", Snackbar.LengthLong).Show();
            else
                RunOnUiThread(() => StartWatcher());
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
            Snackbar.Make(Window!.DecorView, $"Error: {msg}", Snackbar.LengthLong).Show();
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
            RunOnUiThread(() => UpdateUI());
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
                RemoteSystems.Select((x) => $"{x.DisplayName} [{x.Kind}, {(x.Apps.FirstOrDefault()?.IsAvailableBySpatialProximity == true ? "Spacial" : "Fast")}]").ToArray()
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
                try
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
                    else if (Intent?.Action == Intent.ActionSendMultiple)
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

                    FindViewById<View>(Resource.Id.selectDeviceLayout)!.Visibility = ViewStates.Gone;
                    FindViewById<View>(Resource.Id.sendingDataLayout)!.Visibility = ViewStates.Visible;

                    FindViewById<TextView>(Resource.Id.currentDeviceTextView)!.Text = remoteSystem.DisplayName;

                    NearShareStatus result;
                    CircularProgressIndicator progressIndicator = FindViewById<CircularProgressIndicator>(Resource.Id.sendProgressIndicator)!;
                    if (fileTransferOperation != null)
                    {
                        bool requestAccepted = false;
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

                                if (args.TotalFilesToSend != 0 && args.TotalBytesToSend != 0)
                                {
                                    StatusTextView.Text = $"Sending ... {args.FilesSent}/{args.TotalFilesToSend} files ... {Math.Round((decimal)args.BytesSent / args.TotalBytesToSend * 100)}%";
                                    if (!requestAccepted)
                                    {
                                        requestAccepted = true;
                                        FindViewById(Resource.Id.materialCardView1)!.Visibility = ViewStates.Gone;
                                        FindViewById(Resource.Id.progressUILayout)!.Visibility = ViewStates.Visible;
                                    }
                                }
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

                    if (result == NearShareStatus.Completed)
                    {
                        FindViewById(Resource.Id.doneIndicatorImageView)!.Visibility = ViewStates.Visible;
                    }
                    else
                        Snackbar.Make(Window.DecorView, $"Status: {result.Name()}", Snackbar.LengthLong).Show();
                }
                catch (Exception ex)
                {
                    Snackbar.Make(Window.DecorView, $"Error: {ex.Message}", Snackbar.LengthLong).Show();
                }

                await Task.Delay(1500);

                this.Finish();
            }
            else
                Snackbar.Make(Window.DecorView, "Not supported", Snackbar.LengthLong).Show();
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