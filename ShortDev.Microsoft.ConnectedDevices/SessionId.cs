namespace ShortDev.Microsoft.ConnectedDevices;

public record struct SessionId(bool IsHost, uint LocalSessionId, uint RemoteSessionId = 0)
{
    public const ulong SessionIdHostFlag = 0x80000000;

    public static SessionId Parse(ulong sessionId)
    {
        var isHost = (sessionId & SessionIdHostFlag) != 0;

        uint local, remote;
        if (isHost)
        {
            remote = sessionId.HighValue();
            local = sessionId.LowValue() & ~(uint)SessionIdHostFlag;
        }
        else
        {
            local = sessionId.HighValue();
            remote = sessionId.LowValue();
        }

        return new(isHost, local, remote);
    }

    public readonly ulong AsNumber()
    {
        if (IsHost)
            return (ulong)LocalSessionId << 32 | RemoteSessionId | SessionIdHostFlag;

        return (ulong)RemoteSessionId << 32 | LocalSessionId;
    }

    public readonly SessionId WithCorrectedHostFlag()
        => Parse(AsNumber() ^ SessionIdHostFlag);

    public readonly SessionId WithRemoteSessionId(uint remoteSessionId)
        => new(IsHost, LocalSessionId, remoteSessionId);
}
