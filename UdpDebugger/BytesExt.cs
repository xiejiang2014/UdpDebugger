using System;
using System.Collections.Generic;
using System.Linq;

namespace UdpDebugger;

public static class BytesExt
{
    public static string BytesToString
    (
        this IEnumerable<byte> bytes
    )
    {
        var debugString = string.Join
            (
             " , ",
             bytes.Select
                 (
                  v =>
                  {
                      var result = Convert.ToString
                          (
                           v,
                           16
                          );
                      if (result.Length == 1)
                      {
                          result = "0" + result;
                      }

                      return result;
                  }
                 )
            );
        return debugString;
    }


    public static short BytesToShort
    (
        this byte[] bytes
    )
    {
        if (bytes.Length != 2)
        {
            throw new ApplicationException("数据长度必须为2");
        }

        return (short)((bytes[0] << 8) + bytes[1]);
    }

    public static short BytesToShort
    (
        byte byte1, byte byte2
    )
    {
        return (short)((byte2 << 8) + byte1);
    }

    public static byte[] IntToBytes(this int value)
    {
        var src = new byte[4];
        src[3] = (byte)((value >> 24) & 0xFF);
        src[2] = (byte)((value >> 16) & 0xFF);
        src[1] = (byte)((value >> 8)  & 0xFF);
        src[0] = (byte)(value         & 0xFF);
        return src;
    }

    public static byte[] ShortToBytes(this short value)
    {
        var src = new byte[2];
        src[1] = (byte)((value >> 8) & 0xFF);
        src[0] = (byte)(value        & 0xFF);
        return src;
    }
}