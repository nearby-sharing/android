using ShortDev.Microsoft.ConnectedDevices.Transports.WiFiDirect;

namespace ShortDev.Microsoft.ConnectedDevices.Test.Transports;

public class WiFiDirectTests
{
    [Theory]
    [InlineData("MYSSID", "passphrase", "59e0d07fa4c7741797a4e394f38a5c321e3bed51d54ad5fcbd3f84bc7415d73d")]
    [InlineData("DIRECT-IDSomePcTest", "345678uh4w6r78c30r98to7c", "d869bb00d690e4f6b25b44ec9d6e0ab5d584756ee4dcb439f30748c0b00d8e3c")]
    [InlineData("DIRECT-IDSOMEPCTEST", "345678uh4w6r78c30r98to7c", "a21bc6baf7603862a5d41b25ee5091bc5fae1c529fc62359cf25b3f0b80ef35d")]
    public void CreateGroupInfo_ShouldCalculatePsk(string ssid, string passphrase, string expected)
    {
        // INFO: Expected passphrase will be generated using "wpa_passphrase"

        var groupInfo = GroupInfo.Create(ssid, passphrase);
        Assert.Equal(32, groupInfo.PreSharedKey.Length);

        var psk = Convert.ToHexString(groupInfo.PreSharedKey.Span);
        Assert.Equal(expected, psk, ignoreCase: true);
    }
}
