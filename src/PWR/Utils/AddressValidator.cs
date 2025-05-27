using System;
using System.Text.RegularExpressions;

namespace PWR.Utils
{
    public static class AddressValidator
    {
        private static readonly Regex AddressRegex = new Regex("^0x[0-9a-fA-F]{40}$");

        public static bool IsValidAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;

            return AddressRegex.IsMatch(address);
        }

        public static void ValidateAddress(string address)
        {
            if (!IsValidAddress(address))
                throw new ArgumentException("Invalid address format. Address must be a 20-byte hex string prefixed with '0x'");
        }
    }
} 