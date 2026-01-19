using Android.Bluetooth;
using Android.Content;
using Android.Views;
using Google.Android.Material.TextField;
using NearShare.Utils;
using ShortDev.Android.Views;
using ShortDev.Microsoft.ConnectedDevices;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;

namespace NearShare.Receive;

public sealed class ReceiveSetupFragment : Fragment
{
    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        => inflater.Inflate(Resource.Layout.fragment_receive_setup, container, false);

    ViewBindings _viewBindings = null!;
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        _viewBindings = new(view);

        var ctx = RequireContext();
        var preferences = GetPreferences(ctx);

        var service = (BluetoothManager)ctx.GetSystemService(Context.BluetoothService)!;
        string? btAddress = null;
        try
        {
            btAddress = GetBtAddressInternal(service.Adapter!);
        }
        catch
        {
            // ToDo: Display error in UI
            btAddress = preferences.GetString(Preference_MacAddress, null);
        }

        _viewBindings.InfoTextView.TextFormatted = UIHelper.LoadHtmlAsset(ctx, "MacAddressInfo");
        _viewBindings.LaunchSettingsButton.Click += (s, e) => StartActivity(new Intent(Android.Provider.Settings.ActionDeviceInfoSettings));

        _viewBindings.InputLayout.EditText!.Text = btAddress;

        _viewBindings.SaveButton.Click += (s, e) =>
        {
            var addressStr = _viewBindings.InputLayout.EditText!.Text;
            if (
                string.IsNullOrEmpty(addressStr) ||
                !PhysicalAddress.TryParse(addressStr?.Replace(":", "").ToUpper(), out var address) ||
                address == null
            )
            {
                _viewBindings.InputLayout.Error = "Invalid address!";
            }
            else
            {
                _viewBindings.InputLayout.Error = null;

                preferences?.Edit()!
                    .PutString(Preference_MacAddress, address.ToStringFormatted())!
                    .Commit();

                this.NavController.NavigateUp();
            }
        };
    }

    const string Preference_MacAddress = "local_mac_address";

    static string? GetBtAddressInternal(BluetoothAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);

        var mServiceField = adapter.Class.GetDeclaredFields().FirstOrDefault((x) => x.Name.Contains("service", StringComparison.OrdinalIgnoreCase)) ?? throw new MissingFieldException("No service field found!");
        mServiceField.Accessible = true;
        var serviceProxy = mServiceField.Get(adapter)!;
        var method = serviceProxy.Class.GetDeclaredMethod("getAddress") ?? throw new MissingMethodException("No method \"getAddress\"");
        method.Accessible = true;
        try
        {
            return (string?)method.Invoke(serviceProxy);
        }
        catch (Java.Lang.Reflect.InvocationTargetException ex)
        {
            if (ex.Cause == null)
                throw;

            throw ex.Cause;
        }
    }

    static ISharedPreferences GetPreferences(Context context)
        => context.GetSharedPreferences("mac_settings", FileCreationMode.MultiProcess)!;

    public static bool IsSetupRequired(Context context)
        => string.IsNullOrEmpty(GetPreferences(context).GetString(Preference_MacAddress, null));

    public static bool TryGetBtAddress(Context context, [NotNullWhen(true)] out PhysicalAddress? btAddress)
    {
        btAddress = null;

        var addressStr = GetPreferences(context).GetString(Preference_MacAddress, null);
        if (string.IsNullOrEmpty(addressStr))
            return false;

        return PhysicalAddress.TryParse(addressStr.Replace(":", "").ToUpper(), out btAddress);
    }

    sealed class ViewBindings(View view)
    {
        public TextView InfoTextView { get; } = view.FindRequiredViewById<TextView>(Resource.Id.infoTextView);
        public Button LaunchSettingsButton { get; } = view.FindRequiredViewById<Button>(Resource.Id.launchSettingsButton);
        public TextInputLayout InputLayout { get; } = view.FindRequiredViewById<TextInputLayout>(Resource.Id.btMacAddressTextInputLayout);
        public Button SaveButton { get; } = view.FindRequiredViewById<Button>(Resource.Id.saveButton);
    }
}
