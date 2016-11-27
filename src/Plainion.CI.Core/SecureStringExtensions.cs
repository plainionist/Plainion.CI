using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Plainion.CI
{
    public static class SecureStringExtensions
    {
        public static string ToUnsecureString( this SecureString source )
        {
            if ( source == null )
            {
                return null;
            }

            var returnValue = IntPtr.Zero;
            try
            {
                returnValue = Marshal.SecureStringToGlobalAllocUnicode( source );
                return Marshal.PtrToStringUni( returnValue );
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode( returnValue );
            }
        }

        public static SecureString ToSecureString( this string source )
        {
            if ( source == null )
            {
                return null;
            }

            var securePassword = new SecureString();

            foreach ( char c in source )
            {
                securePassword.AppendChar( c );
            }

            securePassword.MakeReadOnly();

            return securePassword;
        }
    }
}
