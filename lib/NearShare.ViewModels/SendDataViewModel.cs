using CommunityToolkit.Mvvm.ComponentModel;
using ShortDev.Microsoft.ConnectedDevices;
using ShortDev.Microsoft.ConnectedDevices.NearShare;
using ShortDev.Microsoft.ConnectedDevices.Transports;

namespace NearShare.ViewModels;

public sealed partial class SendDataViewModel : ObservableObject
{
    readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
    readonly NearShareSender _sender;

    public CdpDevice Device { get; }

    readonly Func<Task> _taskFactory;
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly Progress<NearShareProgress>? _progress;
    private SendDataViewModel(NearShareSender sender, CdpDevice device, Func<Task> taskFactory, CancellationTokenSource cancellationTokenSource, Progress<NearShareProgress>? progress)
    {
        _sender = sender;
        Device = device;
        _taskFactory = taskFactory;
        _cancellationTokenSource = cancellationTokenSource;
        _progress = progress;
        CurrentTransportType = device.Endpoint.TransportType;
    }

    public bool IsMobile => Device.Type.IsMobile();

    [ObservableProperty]
    public partial CdpTransportType CurrentTransportType { get; private set; }

    [ObservableProperty]
    public partial States State { get; private set; } = States.InProgress;

    public bool HasProgress => _progress is not null;

    [ObservableProperty]
    public partial int ProgressBytes { get; private set; }

    [ObservableProperty]
    public partial int TotalBytes { get; private set; }

    [ObservableProperty]
    public partial int TotalFiles { get; private set; }

    [ObservableProperty]
    public partial Exception? Error { get; private set; }

    public void Cancel() => _cancellationTokenSource.Cancel();

    public async void Start()
    {
        Error = null;
        State = States.InProgress;

        _sender.TransportUpgraded += OnTransportUpgrade;
        _progress?.ProgressChanged += OnProgress;

        try
        {
            await _taskFactory();
            State = States.Succeeded;
        }
        catch (OperationCanceledException)
        {
            State = States.Cancelled;
        }
        catch (Exception ex)
        {
            Error = ex;
            State = States.Failed;
        }
        finally
        {
            _sender.TransportUpgraded -= OnTransportUpgrade;
            _progress?.ProgressChanged -= OnProgress;
        }
    }

    private void OnTransportUpgrade(object? sender, CdpTransportType e)
    {
        _synchronizationContext.Post(() =>
        {
            CurrentTransportType = e;
        });
    }

    private void OnProgress(object? sender, NearShareProgress args)
    {
        _synchronizationContext.Post(() =>
        {
            TotalFiles = (int)args.TotalFiles;
            TotalBytes = (int)args.TotalBytes;
            ProgressBytes = (int)args.TransferedBytes;
        });
    }
}
