using Android.Content;
using ShortDev.Microsoft.ConnectedDevices.NearShare;

namespace NearShare.Utils;

internal static class IntentParser
{
    public static ParseShareIntentResult? ShareIntent(Context context, Intent? intent)
    {
        var resolver = context.ContentResolver!;

        if (intent == null)
            return null;

        if (intent.Action is Intent.ActionProcessText)
        {
            var processText = GetTextExtra(intent, Intent.ExtraProcessText);
            if (string.IsNullOrEmpty(processText))
                return null;

            return ShareItem.FromText(processText) switch
            {
                ShareItem.Text(var text) => FileFromString(text),
                ShareItem.Url(var uri) => uri,
            };
        }

        if (intent.Action is not (Intent.ActionSend or Intent.ActionSendMultiple))
            return null;

        HashSet<ShareItem> items = [];

        if (intent.ClipData is { } clipData)
        {
            foreach (var item in Enumerate(clipData))
            {
                items.Add(item switch
                {
                    { Uri: { } uri } => ShareItem.Create(uri),
                    _ => ShareItem.Create(item.CoerceToText(context) ?? ""),
                });
            }
        }

        foreach (var uri in intent.GetParcelableArrayListExtra<AndroidUri>(Intent.ExtraStream) ?? [])
            items.Add(ShareItem.Create(uri));

        foreach (var text in GetTextList(intent, Intent.ExtraText) ?? [])
        {
            if (string.IsNullOrEmpty(text))
                continue;

            items.Add(ShareItem.Create(text));
        }

        if (intent.GetParcelableExtra<AndroidUri>(Intent.ExtraStream) is { } extraUri)
            items.Add(ShareItem.Create(extraUri));

        if (GetTextExtra(intent, Intent.ExtraText) is { Length: > 0 } extraText)
            items.Add(ShareItem.Create(extraText));

        if (intent.Data is { } data)
            items.Add(ShareItem.Create(data));

        return items.ToList() switch
        {
            [] => null,
            [ShareItem.Url(var singleUri)] => singleUri,
            _ => items.Select(x => x switch
            {
                ShareItem.Text(var text) => FileFromString(text),
                ShareItem.Url(var uri) => FileFromString(uri.OriginalString),
                ShareItem.File(var file) => resolver.CreateNearShareFileFromContentUri(file),
            }).ToArray()
        };

        static string? GetTextExtra(Intent intent, string key)
        {
            if (!intent.HasExtra(key))
                return null;

            return intent.GetStringExtra(key) ?? intent.GetCharSequenceExtra(key)?.ToString();
        }

        static IEnumerable<string?>? GetTextList(Intent intent, string key)
        {
            if (!intent.HasExtra(key))
                return null;

            var strings = intent.GetStringArrayListExtra(key);
            if (strings is not null)
                return strings.Cast<string?>();

            var charSequences = intent.GetCharSequenceArrayListExtra(key);
            return charSequences?.Select(value => value?.ToString());
        }

        static IEnumerable<ClipData.Item> Enumerate(ClipData clipData)
        {
            for (int i = 0; i < clipData.ItemCount; i++)
            {
                var item = clipData.GetItemAt(i);
                if (item != null)
                    yield return item;
            }
        }

        static CdpFileProvider FileFromString(string value)
        {
            return CdpFileProvider.FromContent(
                $"transfer-{DateTime.Now:dd_MM_yyyy-HH_mm_ss}.txt",
                value
            );
        }
    }

    private closed record ShareItem
    {
        public sealed record Text(string Value) : ShareItem;
        public sealed record Url(Uri Value) : ShareItem;
        public sealed record File(AndroidUri Uri) : ShareItem
        {
            public bool Equals(File? other)
                => other?.Uri.Equals(Uri) == true;

            public override int GetHashCode()
                => Uri.GetHashCode();
        }

        public static ShareItem Create(string value)
            => FromText(value) switch
            {
                Text text => text,
                Url url => url,
            };

        public static ShareItem Create(AndroidUri value)
        {
            if (IsContentUri(value))
                return new File(value);

            return new Url(new Uri(value.ToString()!));
        }

        static bool IsContentUri(AndroidUri uri) =>
            string.Equals(uri.Scheme, ContentResolver.SchemeContent, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(uri.Scheme, ContentResolver.SchemeFile, StringComparison.OrdinalIgnoreCase);

        public static TextOrUrl FromText(string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return new Url(uri);

            return new Text(value);
        }

        public readonly union TextOrUrl(Text, Url);
    }
}

public readonly union ParseShareIntentResult(IReadOnlyList<CdpFileProvider>, CdpFileProvider?, Uri?);
