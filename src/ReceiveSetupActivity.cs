using Android.Bluetooth;
using Android.Content;
using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.TextField;
using NearShare.Utils;
using ShortDev.Microsoft.ConnectedDevices;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;

namespace NearShare;

[Activity(Label = "@string/app_name", Theme = "@style/AppTheme", ConfigurationChanges = UIHelper.ConfigChangesFlags)]
public sealed class ReceiveSetupActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        SetContentView(Resource.Layout.activity_mac_address);
        UIHelper.SetupToolBar(this, GetString(Resource.String.app_titlebar_title_receive_setup));

        var preferences = GetPreferences(this);

        var service = (BluetoothManager)GetSystemService(BluetoothService)!;
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

        FindViewById<TextView>(Resource.Id.infoTextView)!.TextFormatted = UIHelper.LoadHtmlAsset(this, "MacAddressInfo");
        FindViewById<Button>(Resource.Id.launchSettingsButton)!.Click += (s, e) => StartActivity(new Intent(global::Android.Provider.Settings.ActionDeviceInfoSettings));

        var inputLayout = FindViewById<TextInputLayout>(Resource.Id.btMacAddressTextInputLayout)!;
        inputLayout.EditText!.Text = btAddress;

        FindViewById<Button>(Resource.Id.backButton)!.Click += (s, e) => OnBackPressedDispatcher.OnBackPressed();
        FindViewById<Button>(Resource.Id.nextButton)!.Click += (s, e) =>
        {
            var addressStr = inputLayout.EditText!.Text;
            if (
                string.IsNullOrEmpty(addressStr) ||
                !PhysicalAddress.TryParse(addressStr?.Replace(":", "").ToUpper(), out var address) ||
                address == null
            )
            {
                inputLayout.Error = "Invalid address!";
            }
            else
            {
                inputLayout.Error = null;

                preferences?.Edit()!
                    .PutString(Preference_MacAddress, address.ToStringFormatted())!
                    .Commit();

                StartActivity(new Intent(this, typeof(ReceiveActivity)));
                Finish();
            }
        };
    }

    const string Preference_MacAddress = "local_mac_address";

    static string? GetBtAddressInternal(BluetoothAdapter adapter)
    {
        if (adapter == null)
            throw new ArgumentNullException(nameof(adapter));

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

    public override bool OnCreateOptionsMenu(IMenu? menu)
        => UIHelper.OnCreateOptionsMenu(this, menu);

    public override bool OnOptionsItemSelected(IMenuItem item)
        => UIHelper.OnOptionsItemSelected(this, item);
}
