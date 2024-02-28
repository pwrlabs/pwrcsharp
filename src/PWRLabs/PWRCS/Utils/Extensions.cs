namespace PWRCS;

public static class Extensions
{
    public static byte[] HexStringToByteArray(this string hexString)
    {
        hexString = hexString.Replace(" ", string.Empty);

        if (hexString.Length % 2 != 0)
            throw new ArgumentException("The hexadecimal string must have an even number of characters");

        byte[] byteArray = new byte[hexString.Length / 2];
        for (int i = 0; i < byteArray.Length; i++)
        {
            string hexPair = hexString.Substring(i * 2, 2);
            byteArray[i] = Convert.ToByte(hexPair, 16);
        }

        return byteArray;
    }

  

    
}