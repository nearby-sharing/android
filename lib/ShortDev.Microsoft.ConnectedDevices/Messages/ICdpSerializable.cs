namespace ShortDev.Microsoft.ConnectedDevices.Messages;

public interface ICdpSerializable<T> : ICdpWriteable where T : ICdpSerializable<T>
{
    static abstract T Parse(ref EndianReader reader);
    public static bool TryParse(ref EndianReader reader, out T? result, out Exception? error)
        => throw new NotImplementedException();
}

public interface ICdpArraySerializable<T> where T : ICdpArraySerializable<T>
{
    static abstract IReadOnlyList<T> ParseArray(ref EndianReader reader);
    static abstract void WriteArray(EndianWriter writer, IReadOnlyList<T> array);
}

public interface ICdpWriteable
{
    void Write(EndianWriter writer);
}
