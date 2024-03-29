﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace UdpDebugger;

public static class BytesExt
{
    /// <summary>
    /// 将逗号分割的一组16进制字符串值还原为byte数组.
    /// 例"f3 , b3 , 1a , 44 , ea , e7 , 19 , 44"=>
    /// </summary>
    /// <param name="hexString"></param>
    /// <returns></returns>
    public static byte[] HexStringToBytes(this string hexString)
    {
        var hexValues = hexString.Split(',');

        var byteArray = new byte[hexValues.Length];

        for (var i = 0; i < hexValues.Length; i++)
        {
            var value = Convert.ToByte(hexValues[i].Trim(), 16);
            byteArray[i] = value;
        }

        return byteArray;
    }

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

    public static float[] BytesToFloatArray
    (
        this byte[] bytes
    )
    {
        if (bytes.Length == 0)
        {
            return Array.Empty<float>();
        }

        if (bytes.Length % 4 != 0)
        {
            throw new ArgumentException("字节数组的长度不是4的整数倍");
        }

        var result = new float[bytes.Length / 4];


        for (int i = 0, j = 0; i < bytes.Length; i += 4, j++)
        {
            result[j] = BitConverter.ToSingle(bytes, i);
        }

        return result;
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


    /// <summary>
    /// 大小端转换 0x01 0x02 0x03 0x04  -> 0x02 0x01 0x04 0x03
    /// </summary>
    /// <param name="dataBytes"></param>
    public static void EndianConvert(this byte[] dataBytes)
    {
        if (dataBytes.Length == 4)
        {
            var t = dataBytes[0];
            dataBytes[0] = dataBytes[1];
            dataBytes[1] = t;

            t            = dataBytes[2];
            dataBytes[2] = dataBytes[3];
            dataBytes[3] = t;
        }
    }
}