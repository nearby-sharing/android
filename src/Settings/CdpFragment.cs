using NearShare.Utils;

namespace NearShare.Settings;

internal sealed class CdpFragment : SettingsFragment
{
    public override IReadOnlyList<SettingsGroup> Groups => field ??= [
        new SettingsGroup(title: null) {
            new Setting.Card() {
                Icon = new(
                    Resource.Drawable.ic_fluent_window_new_24_regular, Resource.Color.m3_ref_palette_green30,
                    Resource.Drawable.material_shape_cookie_4, Resource.Color.m3_ref_palette_green90
                ),
                Title = Resource.String.open_setup_title,
                Summary = Resource.String.open_setup_summary,
                Click = () => UIHelper.OpenSetup(Activity!)
            },
        },
        new SettingsGroup(Resource.String.category_setup) {
            new Setting.String(SettingsFragment.DeviceNameName) {
                Icon = new(
                    Resource.Drawable.ic_fluent_rename_24_regular, Resource.Color.m3_ref_palette_cyan30,
                    Resource.Drawable.material_shape_cookie_4, Resource.Color.m3_ref_palette_cyan90
                ),
                Title = Resource.String.device_name_title,
                Summary = Resource.String.device_name_summary,
            },
            new Setting.Card() {
                Icon = new(
                    Resource.Drawable.ic_fluent_bluetooth_24_regular, Resource.Color.m3_ref_palette_purple30,
                    Resource.Drawable.material_shape_cookie_4, Resource.Color.m3_ref_palette_purple90
                ),
                Title = Resource.String.goto_mac_address_title,
                Summary = Resource.String.goto_mac_address_summary,
                Click = () => this.NavController.Navigate(Routes.ReceiveSetup)
            },
        },
        new SettingsGroup(Resource.String.category_permissions) {
            new Setting.Card() {
                Icon = new(
                    Resource.Drawable.ic_fluent_send_copy_24_regular, Resource.Color.m3_ref_palette_blue_variant30,
                    Resource.Drawable.material_shape_cookie_4, Resource.Color.m3_ref_palette_blue_variant90
                ),
                Title = Resource.String.generic_send,
                Summary = Resource.String.request_permissions_summary,
                Click = () => UIHelper.RequestSendPermissions(Activity!)
            },
            new Setting.Card() {
                Icon = new(
                    Resource.Drawable.ic_fluent_mail_inbox_24_regular, Resource.Color.m3_ref_palette_blue30,
                    Resource.Drawable.material_shape_cookie_4, Resource.Color.m3_ref_palette_blue90
                ),
                Title = Resource.String.generic_receive,
                Summary = Resource.String.request_permissions_summary,
                Click = () => UIHelper.RequestReceivePermissions(Activity!)
            },
        },
    ];
}
