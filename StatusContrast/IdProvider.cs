namespace StatusContrast;

public class NodeIdProvider(uint offset)
{
    private uint _current = offset;

    public uint GetNext()
    {
        return _current++;
    }
}
