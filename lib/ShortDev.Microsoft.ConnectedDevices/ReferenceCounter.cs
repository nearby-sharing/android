namespace ShortDev.Microsoft.ConnectedDevices;

public sealed class ReferenceCounter
{
    uint _value = 0;

    public bool Add()
    {
        while (true)
        {
            var currentValue = Volatile.Read(ref _value);

            if (Interlocked.CompareExchange(ref _value, checked(currentValue + 1), currentValue) == currentValue)
                return currentValue == 0;
        }
    }

    public bool Release()
    {
        while (true)
        {
            var currentValue = Volatile.Read(ref _value);
            if (currentValue == 0)
                return false;

            if (Interlocked.CompareExchange(ref _value, checked(currentValue - 1), currentValue) == currentValue)
                return currentValue == 1;
        }
    }
}
