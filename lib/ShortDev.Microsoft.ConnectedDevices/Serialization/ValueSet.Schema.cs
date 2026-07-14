namespace ShortDev.Microsoft.ConnectedDevices.Serialization;

public enum PropertyType
{
    PropertyType_Empty = unchecked((int)0),
    PropertyType_UInt8 = unchecked((int)1),
    PropertyType_Int16 = unchecked((int)2),
    PropertyType_UInt16 = unchecked((int)3),
    PropertyType_Int32 = unchecked((int)4),
    PropertyType_UInt32 = unchecked((int)5),
    PropertyType_Int64 = unchecked((int)6),
    PropertyType_UInt64 = unchecked((int)7),
    PropertyType_Float = unchecked((int)8),
    PropertyType_Double = unchecked((int)9),
    PropertyType_Char = unchecked((int)10),
    PropertyType_Boolean = unchecked((int)11),
    PropertyType_WString = unchecked((int)12),
    PropertyType_DateTimeOffset = unchecked((int)13),
    PropertyType_TimeSpan = unchecked((int)14),
    PropertyType_Guid = unchecked((int)15),
    PropertyType_Point = unchecked((int)16),
    PropertyType_Size = unchecked((int)17),
    PropertyType_Rect = unchecked((int)18),
    PropertyType_ValueSet = unchecked((int)19),
    PropertyType_UInt8Array = unchecked((int)20),
    PropertyType_Int16Array = unchecked((int)21),
    PropertyType_UInt16Array = unchecked((int)22),
    PropertyType_Int32Array = unchecked((int)23),
    PropertyType_UInt32Array = unchecked((int)24),
    PropertyType_Int64Array = unchecked((int)25),
    PropertyType_UInt64Array = unchecked((int)26),
    PropertyType_FloatArray = unchecked((int)27),
    PropertyType_DoubleArray = unchecked((int)28),
    PropertyType_CharArray = unchecked((int)29),
    PropertyType_BooleanArray = unchecked((int)30),
    PropertyType_WStringArray = unchecked((int)31),
    PropertyType_DateTimeOffsetArray = unchecked((int)32),
    PropertyType_TimeSpanArray = unchecked((int)33),
    PropertyType_GuidArray = unchecked((int)34),
    PropertyType_PointArray = unchecked((int)35),
    PropertyType_SizeArray = unchecked((int)36),
    PropertyType_RectArray = unchecked((int)37),
    PropertyType_String = unchecked((int)39),
    PropertyType_StringArray = unchecked((int)40),
}

public partial class UUID
{
    public uint Data1 { get; set; }
    public ushort Data2 { get; set; }
    public ushort Data3 { get; set; }
    public ulong Data4 { get; set; }
}

public partial class Point
{
    public float X { get; set; }
    public float Y { get; set; }
}

public partial class Size
{
    public float Width { get; set; }
    public float Height { get; set; }
}

public partial class Rect
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}

public partial struct PropertyValue()
{
    public PropertyType Type { get; set; } = PropertyType.PropertyType_Empty;

    public byte UInt8Value { get; set; }
    public short Int16Value { get; set; }
    public ushort UInt16Value { get; set; }
    public int Int32Value { get; set; }
    public uint UInt32Value { get; set; }
    public long Int64Value { get; set; }
    public ulong UInt64Value { get; set; }
    public float FloatValue { get; set; }
    public double DoubleValue { get; set; }
    public ushort CharValue { get; set; }
    public bool BooleanValue { get; set; }
    public string WStringValue { get; set; } = "";
    public long DateTimeOffsetValue { get; set; }
    public long TimeSpanValue { get; set; }
    public UUID? GuidValue { get; set; } = null;
    public Point? PointValue { get; set; } = null;
    public Size? SizeValue { get; set; } = null;
    public Rect? RectValue { get; set; } = null;
    public ValueSet? ValueSetValue { get; set; } = null;

    public string StringValue { get; set; } = "";

    public List<byte> UInt8ArrayValue { get; set; } = [];
    public List<short> Int16ArrayValue { get; set; } = [];
    public List<ushort> UInt16ArrayValue { get; set; } = [];
    public List<int> Int32ArrayValue { get; set; } = [];
    public List<uint> UInt32ArrayValue { get; set; } = [];
    public List<long> Int64ArrayValue { get; set; } = [];
    public List<ulong> UInt64ArrayValue { get; set; } = [];
    public List<float> FloatArrayValue { get; set; } = [];
    public List<double> DoubleArrayValue { get; set; } = [];
    public List<ushort> CharArrayValue { get; set; } = [];
    public List<byte> BooleanArrayValue { get; set; } = [];
    public List<string> WStringArrayValue { get; set; } = [];
    public List<long> DateTimeOffsetArrayValue { get; set; } = [];
    public List<long> TimeSpanArrayValue { get; set; } = [];
    public List<UUID> GuidArrayValue { get; set; } = [];
    public List<Point> PointArrayValue { get; set; } = [];
    public List<Size> SizeArrayValue { get; set; } = [];
    public List<Rect> RectArrayValue { get; set; } = [];

    public List<string> StringArrayValue { get; set; } = [];
}

public partial class ValueSet : Dictionary<string, PropertyValue>
{
    public IDictionary<string, PropertyValue> Entries => this;
}
