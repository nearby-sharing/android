using AndroidX.AppCompat.App;
using NearShare.Utils;

namespace NearShare.Settings;

internal sealed class AppearanceFragment : SettingsFragment
{
    public override IReadOnlyList<SettingsGroup> Groups => field ??= [
        new SettingsGroup(title: null) {
            new Setting.Bool(SettingsFragment.ForceDarkModeName) {
                Icon = new(
                    Resource.Drawable.ic_fluent_weather_moon_24_regular, Resource.Color.m3_ref_palette_cyan30,
                    Resource.Drawable.material_shape_pentagon, Resource.Color.m3_ref_palette_cyan90
                ),
                Title = Resource.String.force_dark_mode_title,
                Summary = Resource.String.force_dark_mode_summary,
                OnChange = (value) => AppCompatDelegate.DefaultNightMode = value ? AppCompatDelegate.ModeNightYes : AppCompatDelegate.ModeNightFollowSystem,
            },
            new Setting.Card() {
                Icon = new(
                    Resource.Drawable.ic_fluent_local_language_24_regular, Resource.Color.m3_ref_palette_cyan30,
                    Resource.Drawable.material_shape_pentagon, Resource.Color.m3_ref_palette_cyan90
                ),
                Title = Resource.String.switch_language_title,
                Summary = Resource.String.switch_language_summary,
                Click = () => UIHelper.OpenLocaleSettings(Activity!),
            },
        }
    ];
}
