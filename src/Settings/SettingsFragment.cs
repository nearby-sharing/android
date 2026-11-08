using Android.Bluetooth;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using AndroidX.Core.Content;
using AndroidX.Preference;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Dialog;
using Google.Android.Material.ListItem;
using Google.Android.Material.MaterialSwitch;
using NearShare.Utils;
using ShortDev.Android.Graphics;
using ShortDev.Android.UI;
using ShortDev.Android.Views;
using System.Diagnostics;
using static AndroidX.RecyclerView.Widget.RecyclerView;

namespace NearShare.Settings;

internal abstract class SettingsFragment : Fragment
{
    internal const string ForceDarkModeName = "force_dark_mode";
    internal const string DeviceNameName = "device_name";

    public static ISharedPreferences GetPreferences(Context context)
        => PreferenceManager.GetDefaultSharedPreferences(context)!;

    public static bool ShouldForceDarkMode(Context context)
        => GetPreferences(context).GetBoolean(ForceDarkModeName, false);

    public static string GetDeviceName(Context context, BluetoothAdapter adapter)
    {
        var deviceName = GetPreferences(context).GetString(DeviceNameName, null);
        if (string.IsNullOrEmpty(deviceName))
            deviceName = adapter.Name;

        return deviceName ?? Environment.MachineName;
    }

    public abstract IReadOnlyList<SettingsGroup> Groups { get; }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        => inflater.Inflate(Resource.Layout.fragment_settings, container, attachToRoot: false);

    RecyclerView? _recyclerView;
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        _recyclerView = view.FindRequiredViewById<RecyclerView>(Resource.Id.recycler_view);
        _recyclerView.SetLayoutManager(new LinearLayoutManager(RequireContext()));
        _recyclerView.AddItemDecoration(new MarginItemDecoration(RequireContext()));

        _recyclerView.SetAdapter(new MultiViewAdapter<SettingsItem>()
        {
            ItemsSource = [.. Groups.SelectMany(FlattenGroup)],
            ViewTypeResolver = (item) => item switch
            {
                SettingsItem.SubHeading => Resource.Layout.settings_item_subheader,
                SettingsItem.SettingWrapper => Resource.Layout.settings_item_viewholder,
            },
            LayoutSelector = (viewType) => viewType,
            ViewHolderFactory = (view, viewType) => viewType switch
            {
                Resource.Layout.settings_item_subheader => new SubHeaderViewHolder(view),
                Resource.Layout.settings_item_viewholder => new SettingViewHolder(view),
                _ => throw new UnreachableException()
            },
        });

        static IEnumerable<SettingsItem> FlattenGroup(SettingsGroup group)
        {
            if (group.Title is { } title)
                yield return new SettingsItem.SubHeading(title);

            foreach (var item in group)
                yield return new SettingsItem.SettingWrapper(item, group);
        }
    }

    private closed record SettingsItem
    {
        public sealed record SubHeading(StringOrResource Title) : SettingsItem;
        public sealed record SettingWrapper(Setting Value, SettingsGroup Group) : SettingsItem;
    }

    private sealed class SubHeaderViewHolder(View view) : ViewHolder<SettingsItem>(view)
    {
        private readonly View _view = view;
        private readonly TextView _labelTextView = view.RequireViewById<TextView>(Resource.Id.settings_item_subheader_text);

        public override void Bind(int index, SettingsItem item)
        {
            _labelTextView.Text = (item as SettingsItem.SubHeading)?.Title.ToString(_view.Context);
        }
    }

    private sealed class SettingViewHolder(View view) : ViewHolder<SettingsItem>(view)
    {
        private readonly View _view = view;
        private readonly ListItemCardView _card = view.FindRequiredViewById<ListItemCardView>(Resource.Id.settings_item_card);
        private readonly ImageView _icon = view.FindRequiredViewById<ImageView>(Resource.Id.settings_item_icon);
        private readonly TextView _label = view.FindRequiredViewById<TextView>(Resource.Id.settings_item_label);
        private readonly TextView _supportingText = view.FindRequiredViewById<TextView>(Resource.Id.settings_item_supporting_text);
        private readonly MaterialSwitch _switch = view.FindRequiredViewById<MaterialSwitch>(Resource.Id.settings_item_switch);
        private readonly View _editIndicator = view.FindRequiredViewById<View>(Resource.Id.settings_item_edit_indicator);

        Setting? _setting;
        public override void Bind(int index, SettingsItem item)
        {
            if (item is not SettingsItem.SettingWrapper { Value: var setting, Group: var group })
                return;

            _setting = setting;
            (_view as ListItemLayout)?.UpdateAppearance(group.IndexOf(setting), group.Count);

            _card.Click += OnClick;

            _setting.Icon.Apply(_icon);
            _label.Text = _setting.Title.ToString(_view.Context);
            _supportingText.Text = _setting.Summary?.ToString(_view.Context);
            _supportingText.Visibility = !string.IsNullOrEmpty(_supportingText.Text) ? ViewStates.Visible : ViewStates.Gone;

            _switch.Visibility = (setting is Setting.Bool) ? ViewStates.Visible : ViewStates.Gone;
            if (setting is Setting.Bool boolSetting)
                _switch.Checked = boolSetting.GetValue(_switch.Context);
            _switch.CheckedChange += OnCheckedChange;

            _editIndicator.Visibility = (setting is Setting.String) ? ViewStates.Visible : ViewStates.Gone;
        }

        public override void Recycle()
        {
            _view.Click -= OnClick;
            _switch.CheckedChange -= OnCheckedChange;
        }

        private void OnClick(object? sender, EventArgs e)
        {
            switch (_setting)
            {
                case Setting.Card card:
                    card.Click?.Invoke();
                    break;

                case Setting.Bool boolSetting:
                    _switch.Checked = !_switch.Checked;
                    boolSetting.SetValue(_switch.Context, _switch.Checked);
                    break;

                case Setting.String stringSetting:
                    try
                    {
                        var editText = (EditText)LayoutInflater.From(_view.Context).Inflate(Resource.Layout.dialog_view_edittext, root: null, attachToRoot: false)!;
                        editText.Text = stringSetting.GetValue(_view.Context);

                        new MaterialAlertDialogBuilder(_view.Context)
                            .SetTitle(stringSetting.Title.ToString(_view.Context))!
                            .SetMessage(stringSetting.Summary?.ToString(_view.Context))!
                            .SetView(editText)!
                            .SetPositiveButton(Resource.String.generic_save, (s, e) =>
                            {
                                stringSetting.SetValue(_view.Context, editText.Text);
                            })
                            .SetNegativeButton(Resource.String.generic_cancel, (s, e) => { })
                            .Show();
                    }
                    catch (Exception ex)
                    {
                        SentrySdk.CaptureException(ex);
                    }
                    break;
            }
        }

        private void OnCheckedChange(object? sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (_setting is Setting.Bool boolSetting)
                boolSetting.OnChange?.Invoke(e.IsChecked);
        }
    }

    private sealed class MarginItemDecoration(Context context) : ItemDecoration
    {
        private readonly int _itemMargin = context.Resources.GetDimensionPixelSize(Resource.Dimension.list_item_margin);

        public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
        {
            var position = parent.GetChildAdapterPosition(view);
            bool isHeading = parent.FindViewHolderForAdapterPosition(position) is SubHeaderViewHolder;
            if (!isHeading && position != state.ItemCount - 1)
                outRect.Bottom = _itemMargin;
        }
    }
}

internal sealed class SettingsGroup(StringOrResource? title) : List<Setting>
{
    public StringOrResource? Title { get; } = title;
}

internal closed record Setting
{
    public required SettingsIcon Icon { get; init; }

    public required StringOrResource Title { get; init; }
    public StringOrResource? Summary { get; init; }

    public sealed record Card : Setting
    {
        public Action? Click { get; init; }
    }

    public sealed record Bool(string Key) : Setting
    {
        public bool DefaultValue { get; init; }
        public Action<bool>? OnChange { get; init; }

        public bool GetValue(Context context)
            => SettingsFragment.GetPreferences(context).GetBoolean(Key, defValue: false);

        public void SetValue(Context context, bool value)
            => SettingsFragment.GetPreferences(context).Edit()!
                .PutBoolean(Key, value)!
                .Commit();
    }

    public sealed record String(string Key) : Setting
    {
        public Action<string>? OnChange { get; init; }

        public string GetValue(Context context)
            => SettingsFragment.GetPreferences(context).GetString(Key, defValue: "") ?? "";

        public void SetValue(Context context, string? value)
            => SettingsFragment.GetPreferences(context).Edit()!
                .PutString(Key, value)!
                .Commit();
    }
}

internal readonly record struct SettingsIcon(int Icon, int IconTint, int Background, int BackgroundTint)
{
    public void Apply(ImageView image)
    {
        image.SetImageResource(Icon);
        image.ImageTintList = ColorStateList.ValueOf(ContextCompat.GetColor(image.Context, IconTint).AsColor());
        image.SetBackgroundResource(Background);
        image.BackgroundTintList = ColorStateList.ValueOf(ContextCompat.GetColor(image.Context, BackgroundTint).AsColor());
    }
}

internal readonly union StringOrResource(int, string)
{
    public readonly string ToString(Context context)
    {
        return this switch
        {
            string text => text,
            int resId => context.Localize(resId)
        };
    }
}
