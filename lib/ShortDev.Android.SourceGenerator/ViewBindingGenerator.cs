using Microsoft.CodeAnalysis;
using System.Text;

namespace ShortDev.Android.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class ViewBindingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var layoutFiles = context.AdditionalTextsProvider.Where(static x => (x.Path.Contains("Resources/layout/") || x.Path.Contains("Resources\\layout\\")) && x.Path.EndsWith(".xml"));

        IncrementalValuesProvider<string> contents = layoutFiles
                .Select((text, cancellationToken) => text.GetText(cancellationToken)?.ToString() ?? throw new NullReferenceException("Empty SourceText"));

        context.RegisterSourceOutput(contents, (ctx, content) =>
        {
            // ctx.AddSource()
        });
    }

    // mirror-goog-studio-main:android/src/com/android/tools/idea/databinding/util/DataBindingUtil.java
    static string FileToClassName(string fileName)
    {
        ReadOnlySpan<char> fileNameRaw = fileName;

        int dotIndex = fileNameRaw.IndexOf('.');
        if (dotIndex >= 0)
            fileNameRaw = fileNameRaw[..dotIndex];

        StringBuilder builder = new();
        while (true)
        {
            var index = fileNameRaw.IndexOfAny(['_', '-']);
            if (index < 0)
                break;

            Capitalize(builder, fileNameRaw[..index]);
        }
        return builder.ToString();
    }

    static void Capitalize(StringBuilder builder, ReadOnlySpan<char> value)
    {
        switch (value.Length)
        {
            case 0:
                return;

            case 1:
                builder.Append(char.ToUpperInvariant(value[0]));
                return;

            case > 1:
                builder.Append(char.ToUpperInvariant(value[0]));
                builder.Append(value[1..]);
                return;
        }
    }
}
