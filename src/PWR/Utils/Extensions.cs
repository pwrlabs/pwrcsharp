using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PWR.Utils;

public static class Extensions
{
    public static byte[] HexStringToByteArray(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            throw new ArgumentException("Hex string cannot be null or empty");

        hex = hex.StartsWith("0x") ? hex.Substring(2) : hex;

        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even length");

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    public static string ToHex(this byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    public static bool IsHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return false;

        // Remove "0x" prefix if present
        hex = hex.StartsWith("0x") ? hex.Substring(2) : hex;

        return Regex.IsMatch(hex, @"^[0-9a-fA-F]+$");
    }
}