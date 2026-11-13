namespace ShortDev.Microsoft.ConnectedDevices.AppControl.Messages;

public readonly struct CallAppServiceRequest() : IBinaryWritable<CallAppServiceRequest>, IBinaryParsable<CallAppServiceRequest>
{
    public static CallAppServiceRequest Parse<TReader>(ref TReader reader) where TReader : struct, IEndianReader, allows ref struct
    {
        var packageName = reader.ReadStringWithLength();
        var appServiceName = reader.ReadStringWithLength();

        byte[] data = new byte[reader.ReadUInt32()];
        reader.ReadBytes(data);

        var dataFormat = (InputMessageFormat)reader.ReadUInt8();
        return new CallAppServiceRequest
        {
            PackageName = packageName,
            AppServiceName = appServiceName,
            Data = data,
            DataFormat = dataFormat
        };
    }

    /// <summary>
    /// The package name, of the app that hosts the app service.
    /// </summary>
    public required string PackageName { get; init; }

    /// <summary>
    /// The name, of the app service. 
    /// </summary>
    public required string AppServiceName { get; init; }

    /// <summary>
    /// The list of parameters that is sent to the app service for execution.
    /// </summary>
    public required ReadOnlyMemory<byte> Data { get; init; }

    /// <summary>
    /// Specifies the format of <see cref="Data"/>. <br/>
    /// Not supported in client versions earlier than Windows 10 v1809, or in Windows Server 2016.
    /// </summary>
    public required InputMessageFormat DataFormat { get; init; }

    public void Write<TWriter>(ref TWriter writer) where TWriter : struct, IEndianWriter, allows ref struct
    {
        writer.WriteWithLength(PackageName);
        writer.WriteWithLength(AppServiceName);
        writer.Write((uint)Data.Length);
        writer.Write(Data.Span);
        writer.Write((byte)DataFormat);
    }
}

public enum InputMessageFormat : byte
{
    /// <summary>
    /// The input data for the app service is in JSON format.
    /// </summary>
    Json = 0,
    /// <summary>
    /// BOND.NET serialized data.
    /// </summary>
    ValueSet = 1,
}
