using NearShare.Utils;

namespace NearShare.Settings;

internal sealed class SettingsHomepageFragment : SettingsFragment
{
    public override IReadOnlyList<SettingsGroup> Groups => field ??= [
        new SettingsGroup(title: null) {
            new Setting.Card()
            {
                Icon = new(
                    Resource.Drawable.ic_fluent_color_24_regular, Resource.Color.m3_ref_palette_blue_variant30,
                    Resource.Drawable.material_shape_pentagon, Resource.Color.m3_ref_palette_blue_variant90
                ),
                Title = Resource.String.preference_appearance_title,
                Summary = Resource.String.preference_appearance_summary,
                Click = () => this.NavController.Navigate(Routes.SettingsAppearance),
            },
            new Setting.Card()
            {
                Icon = new(
                    Resource.Drawable.ic_fluent_share_24_regular, Resource.Color.m3_ref_palette_green30,
                    Resource.Drawable.material_shape_cookie_4, Resource.Color.m3_ref_palette_green90
                ),
                Title = Resource.String.preference_cdp_title,
                Summary = Resource.String.preference_cdp_summary,
                Click = () => this.NavController.Navigate(Routes.SettingsCdp),
            },
        },

        new SettingsGroup(Resource.String.category_links) {
            new Setting.Card()
            {
                Icon = new(
                    Resource.Drawable.ic_fluent_question_circle_24_regular, Resource.Color.m3_ref_palette_pink30,
                    Resource.Drawable.material_shape_cookie_9, Resource.Color.m3_ref_palette_pink90
                ),
                Title = Resource.String.open_faq_title,
                Summary = Resource.String.open_faq_summary,
                Click = () => UIHelper.OpenFAQ(Activity!),
            },
            new Setting.Card()
            {
                Icon = new(
                    Resource.Drawable.ic_fluent_chat_24_regular, Resource.Color.m3_ref_palette_red30,
                    Resource.Drawable.material_shape_cookie_9, Resource.Color.m3_ref_palette_red90
                ),
                Title = Resource.String.open_discord_title,
                Summary = Resource.String.open_discord_summary,
                Click = () => UIHelper.OpenDiscord(Activity!),
            },
            new Setting.Card()
            {
                Icon = new(
                    Resource.Drawable.ic_fluent_handshake_24_selector, Resource.Color.m3_ref_palette_orange30,
                    Resource.Drawable.material_shape_cookie_9, Resource.Color.m3_ref_palette_orange90
                ),
                Title = Resource.String.open_sponsor_title,
                Summary = Resource.String.open_sponsor_summary,
                Click = () => UIHelper.OpenSponsor(Activity!),
            },
        },

        new SettingsGroup(Resource.String.category_about) {
            new Setting.Card()
            {
                Icon = new(
                    Resource.Drawable.ic_fluent_people_community_24_regular, Resource.Color.m3_ref_palette_cyan30,
                    Resource.Drawable.material_shape_cookie_9, Resource.Color.m3_ref_palette_cyan90
                ),
                Title = Resource.String.show_credits_title,
                Click = () => UIHelper.OpenCredits(Activity!),
            },
            new Setting.Card()
            {
                Icon = new(
                    Resource.Drawable.ic_fluent_code_24_regular, Resource.Color.m3_ref_palette_blue30,
                    Resource.Drawable.material_shape_cookie_9, Resource.Color.m3_ref_palette_blue90
                ),
                Title = "GitHub",
                Click = () => UIHelper.OpenGitHub(Activity!),
            },
        },
    ];
}
