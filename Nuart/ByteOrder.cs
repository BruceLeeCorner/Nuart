// ReSharper disable All

namespace NKit.Uart
{
    public enum ByteOrder2
    {
        AB,
        BA
    }

    public enum ByteOrder4
    {
        ABCD,
        CDAB,
        BADC,
        DCBA
    }

    public enum ByteOrder8
    {
        ABCDEFGH,
        GHEFCDAB,
        BADCFEHG,
        HGFEDCBA
    }
}