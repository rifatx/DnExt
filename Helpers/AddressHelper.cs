using System;

namespace DnExt.Helpers
{
    internal static class AddressHelper
    {
        internal static ulong ConvertHexAddressToUlong(string address) => Convert.ToUInt64(address, 16);
    }
}
