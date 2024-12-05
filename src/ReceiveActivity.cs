using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Dialog;
using Google.Android.Material.ProgressIndicator;
using Microsoft.Extensions.Logging;
using NearShare.Droid.Settings;
using NearShare.Droid.WiFiDirect;
using ShortDev.Android.UI;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.Encryption;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports;
using ShortDev.Microsoft.ConnectedDevices.Transports.Bluetooth;
using ShortDev.Microsoft.ConnectedDevices.Transports.Network;
using ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using SystemDebug = System.Diagnostics.Debug;

namespace NearShare.Droid;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class ReceiveActivity : AppCompatActivity
{
    RecyclerView notificationsRecyclerView = null!;
    readonly ObservableCollection<TransferToken> _notifications = [];

    PhysicalAddress? btAddress = null;

    ILogger<ReceiveActivity> _logger = null!;
    ILoggerFactory _loggerFactory = null!;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (ReceiveSetupActivity.IsSetupRequired(this) || !ReceiveSetupActivity.TryGetBtAddress(this, out btAddress) || btAddress == null)
        {
            StartActivity(new Intent(this, typeof(ReceiveSetupActivity)));

            Finish();
            return;
        }

        SetContentView(Resource.Layout.activity_receive);

        UIHelper.SetupToolBar(this, GetString(Resource.String.app_titlebar_title_receive));

        notificationsRecyclerView = FindViewById<RecyclerView>(Resource.Id.notificationsRecyclerView)!;
        notificationsRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
        notificationsRecyclerView.SetAdapter(
            new AdapterDescriptor<TransferToken>(
                Resource.Layout.item_transfer_notification,
                OnInflateNotification
            ).CreateRecyclerViewAdapter(_notifications)
        );

        FindViewById<Button>(Resource.Id.openFAQButton)!.Click += (s, e) => UIHelper.OpenFAQ(this);

        _loggerFactory = ConnectedDevicesPlatform.CreateLoggerFactory(this.GetLogFilePattern());
        _logger = _loggerFactory.CreateLogger<ReceiveActivity>();

        UIHelper.RequestReceivePermissions(this);
    }

    void OnInflateNotification(View view, TransferToken transfer)
    {
        var acceptButton = view.FindViewById<Button>(Resource.Id.acceptButton)!;
        var openButton = view.FindViewById<Button>(Resource.Id.openButton)!;
        var fileNameTextView = view.FindViewById<TextView>(Resource.Id.fileNameTextView)!;
        var detailsTextView = view.FindViewById<TextView>(Resource.Id.detailsTextView)!;
        var loadingProgressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.loadingProgressIndicator)!;

        view.FindViewById<Button>(Resource.Id.cancelButton)!.Click += (s, e) =>
        {
            if (transfer is FileTransferToken fileTransfer)
                fileTransfer.Cancel();

            _notifications.Remove(transfer);
        };

        if (transfer is UriTransferToken uriTranfer)
        {
            fileNameTextView.Text = uriTranfer.Uri;
            detailsTextView.Text = uriTranfer.DeviceName;

            acceptButton.Visibility = ViewStates.Gone;
            openButton.Visibility = ViewStates.Visible;

            openButton.Click += (s, e) => UIHelper.DisplayWebSite(this, uriTranfer.Uri);

            return;
        }

        if (transfer is not FileTransferToken fileTransfer)
            throw new UnreachableException();

        fileNameTextView.Text = string.Join(", ", fileTransfer.Files.Select(x => x.Name));
        detailsTextView.Text = $"{fileTransfer.DeviceName} • {FileTransferToken.FormatFileSize(fileTransfer.TotalBytes)}";

        acceptButton.Click += OnAccept;

        fileTransfer.Progress += progress => RunOnUiThread(() => OnProgress(progress));
        loadingProgressIndicator.Indeterminate = true;

        openButton.Click += (_, _) =>
        {
            this.ViewDownloads();

            // ToDo: View single file
            // if (fileTransfer.Files.Count == 1)
        };

        UpdateUI();

        void OnAccept(object? sender, EventArgs e)
        {
            try
            {
                var streams = fileTransfer.Select(file => ContentResolver!.CreateMediaStoreStream(file.Name).stream).ToArray();
                fileTransfer.Accept(streams);

                fileTransfer.Finished += () =>
                {
                    // ToDo: Delete failed transfers
                };
            }
            catch (Exception ex)
            {
                new MaterialAlertDialogBuilder(this)
                    .SetTitle(ex.GetType().Name)!
                    .SetMessage(ex.Message)!
                    .Show();
            }

            UpdateUI();
        }

        void OnProgress(NearShareProgress progress)
        {
            loadingProgressIndicator.Indeterminate = false;

            int progressInt = progress.TotalBytes == 0 ? 0 : Math.Min((int)(progress.TransferedBytes * 100 / progress.TotalBytes), 100);
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
                loadingProgressIndicator.SetProgress(progressInt, animate: true);
            else
                loadingProgressIndicator.Progress = progressInt;

            UpdateUI();
        }

        void UpdateUI()
        {
            acceptButton.Visibility = !fileTransfer.IsTransferComplete && !fileTransfer.IsAccepted ? ViewStates.Visible : ViewStates.Gone;
            loadingProgressIndicator.Visibility = !fileTransfer.IsTransferComplete && fileTransfer.IsAccepted ? ViewStates.Visible : ViewStates.Gone;
            openButton.Visibility = fileTransfer.IsTransferComplete ? ViewStates.Visible : ViewStates.Gone;
        }
    }

    CancellationTokenSource? _cancellationTokenSource;
    ConnectedDevicesPlatform? _cdp;
    void InitializeCDP()
    {
        if (btAddress == null)
            throw new NullReferenceException(nameof(btAddress));

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();

        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
        var btAdapter = service.Adapter!;

        var deviceName = SettingsFragment.GetDeviceName(this, btAdapter);

        SystemDebug.Assert(_cdp == null);

        _cdp = new(new()
        {
            Type = DeviceType.Android,
            Name = deviceName,
            OemModelName = Build.Model ?? string.Empty,
            OemManufacturerName = Build.Manufacturer ?? string.Empty,
            DeviceCertificate = ConnectedDevicesPlatform.CreateDeviceCertificate(CdpEncryptionParams.Default)
        }, _loggerFactory);

        IBluetoothHandler bluetoothHandler = new AndroidBluetoothHandler(btAdapter, btAddress);
        _cdp.AddTransport<BluetoothTransport>(new(bluetoothHandler));

        INetworkHandler networkHandler = new AndroidNetworkHandler(this);
        NetworkTransport networkTransport = new(networkHandler);
        _cdp.AddTransport(networkTransport);

        AndroidWiFiDirectHandler wiFiDirectHandler = new(this);
        _cdp.AddTransport<WiFiDirectTransport>(new(wiFiDirectHandler, networkTransport));

        _cdp.Listen(_cancellationTokenSource.Token);
        _cdp.Advertise(_cancellationTokenSource.Token);

        NearShareReceiver.Register(_cdp);
        NearShareReceiver.ReceivedUri += OnTransfer;
        NearShareReceiver.FileTransfer += OnTransfer;

        FindViewById<TextView>(Resource.Id.deviceInfoTextView)!.Text = this.Localize(
            Resource.String.visible_as_template,
            $"""
            "{deviceName}"
            Address: {btAddress.ToStringFormatted()}
            IP-Address: {networkHandler.TryGetLocalIp()?.ToString() ?? "null"}
            """
        );
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        _logger.RequestPermissionResult(requestCode, permissions, grantResults);

        InitializeCDP();
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
        => UIHelper.OnCreateOptionsMenu(this, menu);

    public override bool OnOptionsItemSelected(IMenuItem item)
        => UIHelper.OnOptionsItemSelected(this, item);

    public override void Finish()
    {
        _cancellationTokenSource?.Cancel();
        _cdp?.Dispose();
        NearShareReceiver.Unregister();
        base.Finish();
    }

    void OnTransfer(TransferToken transfer)
        => RunOnUiThread(() => _notifications.Add(transfer));
}

static class Extensions
{
    public static EndpointInfo ToCdp(this BluetoothDevice @this)
        => new(
            CdpTransportType.Rfcomm,
            @this.Address ?? throw new InvalidDataException("Empty address"),
            Constants.RfcommServiceId
        );

    public static CdpSocket ToCdp(this BluetoothSocket @this)
        => new()
        {
            InputStream = @this.InputStream ?? throw new NullReferenceException(),
            OutputStream = @this.OutputStream ?? throw new NullReferenceException(),
            Endpoint = @this.RemoteDevice!.ToCdp(),
            Close = @this.Close
        };
}