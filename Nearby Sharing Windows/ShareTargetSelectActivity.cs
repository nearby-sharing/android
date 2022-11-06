#nullable enable

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Com.Microsoft.Connecteddevices;
using Com.Microsoft.Connecteddevices.Remotesystems;
using Com.Microsoft.Connecteddevices.Remotesystems.Commanding;
using Com.Microsoft.Connecteddevices.Remotesystems.Commanding.Nearshare;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using Java.Util.Concurrent;
using ShortDev.Android.UI;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using AndroidUri = Android.Net.Uri;
using ManifestPermission = Android.Manifest.Permission;

namespace Nearby_Sharing_Windows;

[IntentFilter(new[] { Intent.ActionSend, Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "*/*", Label = "Share file")]
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataMimeType = "text/plain", Label = "Share url")]
[Activity(Label = "@string/app_name", Exported = true, Theme = "@style/AppTheme.TranslucentOverlay", ConfigurationChanges = Constants.ConfigChangesFlags)]
public sealed class ShareTargetSelectActivity : AppCompatActivity, View.IOnApplyWindowInsetsListener
{
    [AllowNull] NearShareSender NearShareSender;

    [AllowNull] RecyclerView DeviceDiscoveryListView;
    [AllowNull] TextView StatusTextView;
    [AllowNull] FrameLayout bottomSheetFrame;
    [AllowNull] Button cancelButton;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        //SentryXamarin.Init(options =>
        //{
        //    options.Dsn = "https://47f9f6c3642149a5af942e8484e64fe1@o646413.ingest.sentry.io/6437134";
        //    options.Debug = true;
        //    options.TracesSampleRate = 1.0;
        //});
        SetContentView(Resource.Layout.activity_share);

        StatusTextView = FindViewById<TextView>(Resource.Id.statusTextView)!;
        bottomSheetFrame = FindViewById<FrameLayout>(Resource.Id.standard_bottom_sheet)!;
        cancelButton = FindViewById<Button>(Resource.Id.cancel_button)!;

        DeviceDiscoveryListView = FindViewById<RecyclerView>(Resource.Id.deviceSelector)!;
        DeviceDiscoveryListView.SetLayoutManager(new GridLayoutManager(this, 2));
        adapterDescriptor = new AdapterDescriptor<RemoteSystem>(
            Resource.Layout.item_device,
            (view, device) =>
            {
                view.FindViewById<ImageView>(Resource.Id.deviceTypeImageView)?.SetImageResource(
                    device.Kind == "Desktop" ? Resource.Drawable.ic_fluent_desktop_20_regular : Resource.Drawable.ic_fluent_phone_20_regular
                );
                view.FindViewById<ImageView>(Resource.Id.transportTypeImageView)?.SetImageResource(
                    device.Apps.FirstOrDefault()?.IsAvailableBySpatialProximity == true ? Resource.Drawable.ic_fluent_bluetooth_20_regular : Resource.Drawable.ic_fluent_wifi_1_20_regular
                );
                view.FindViewById<TextView>(Resource.Id.deviceNameTextView)!.Text = device.DisplayName;
                view.Click += (s, e) => SendData(device);
            }
        );

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
        {
            Window!.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
            Window!.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightNavigationBar;
            Window!.DecorView.SetOnApplyWindowInsetsListener(this);
        }

        cancelButton.Click += CancelButton_Click;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            RequestPermissions(new[] {
                ManifestPermission.AccessFineLocation,
                ManifestPermission.AccessCoarseLocation,
                ManifestPermission.BluetoothScan,
                ManifestPermission.BluetoothConnect
            }, 0);
        }
        else
        {
            RequestPermissions(new[] {
                ManifestPermission.AccessFineLocation,
                ManifestPermission.AccessCoarseLocation
            }, 0);
        }
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

    #region Initialization
    [AllowNull] ConnectedDevicesPlatform Platform { get; set; }
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
    #endregion

    #region Watcher
    [AllowNull] RemoteSystemWatcher Watcher { get; set; }
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
    #endregion

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

    [MaybeNull] AdapterDescriptor<RemoteSystem> adapterDescriptor;
    private void UpdateUI()
    {
        FindViewById<View>(Resource.Id.emptyDeviceListView)!.Visibility = RemoteSystems.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        DeviceDiscoveryListView.SetAdapter(adapterDescriptor!.CreateRecyclerViewAdapter(RemoteSystems));
    }
    #endregion

    AsyncOperationWithProgress? fileTransferOperation = null;
    private async void SendData(RemoteSystem remoteSystem)
    {
        StatusTextView.Text = "Waiting for acceptance...";

        RemoteSystemConnectionRequest connectionRequest = new RemoteSystemConnectionRequest(remoteSystem);
        if (NearShareSender.IsNearShareSupported(connectionRequest))
        {
            try
            {
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
                                OnRequestAccepted();
                            }
#if !DEBUG
                            }
                            catch { }
#endif
                        });
                    };
                    cancelButton.Enabled = true;
                    result = (await fileTransferOperation.GetAsync() as NearShareStatus)!;
                    cancelButton.Enabled = false;
                    fileTransferOperation = null;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(uriTransferOperation != null, "\"uriTransferOperation\" is null!");

                    OnRequestAccepted();
                    result = (await uriTransferOperation!.GetAsync() as NearShareStatus)!;
                }

                if (result == NearShareStatus.Completed)
                {
                    FindViewById(Resource.Id.doneIndicatorImageView)!.Visibility = ViewStates.Visible;
                }
                else
                    Snackbar.Make(Window!.DecorView, $"Status: {result.Name()}", Snackbar.LengthLong).Show();
            }
            catch (Exception ex)
            {
                Snackbar.Make(Window!.DecorView, $"Error: {ex.Message}", Snackbar.LengthLong).Show();
            }

            await Task.Delay(1500);

            this.Finish();
        }
        else
            Snackbar.Make(Window!.DecorView, "Not supported", Snackbar.LengthLong).Show();
    }

    void OnRequestAccepted()
    {
        FindViewById(Resource.Id.loadingProgressIndicator)!.Visibility = ViewStates.Gone;
        FindViewById(Resource.Id.waitForAcceptanceView)!.Visibility = ViewStates.Gone;
        FindViewById(Resource.Id.progressUILayout)!.Visibility = ViewStates.Visible;
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        fileTransferOperation?.Cancel(true);
    }

    #region Finish
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
    #endregion
}