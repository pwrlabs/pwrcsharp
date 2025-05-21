using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Security;

namespace PWR.Utils;

public class DeterministicSecureRandom : SecureRandom
{
    private readonly SHA256 _digest;
    private readonly byte[] _seed;
    private int _counter = 0;

    public DeterministicSecureRandom(byte[] seed)
    {
        _seed = (byte[])seed.Clone();
        _digest = SHA256.Create();
    }

    public override void NextBytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            _digest.Initialize();
            _digest.TransformBlock(_seed, 0, _seed.Length, null, 0);
            
            // Convert counter to bytes
            byte[] counterBytes = BitConverter.GetBytes(_counter++);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }
            
            _digest.TransformBlock(counterBytes, 0, counterBytes.Length, null, 0);
            _digest.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            
            byte[] hash = _digest.Hash;
            int toCopy = Math.Min(hash.Length, bytes.Length - index);
            Array.Copy(hash, 0, bytes, index, toCopy);
            index += toCopy;
        }
    }

    // Required override for SecureRandom
    public override void NextBytes(byte[] buf, int off, int len)
    {
        byte[] temp = new byte[len];
        NextBytes(temp);
        Array.Copy(temp, 0, buf, off, len);
    }

    // Required override for SecureRandom
    public override int NextInt()
    {
        byte[] bytes = new byte[4];
        NextBytes(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    // Required override for SecureRandom
    public override long NextLong()
    {
        byte[] bytes = new byte[8];
        NextBytes(bytes);
        return BitConverter.ToInt64(bytes, 0);
    }
}