using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Plainion.Serialization;

namespace Plainion.CI
{
    [Serializable]
    [DataContract( Namespace = "http://github.com/ronin4net/plainion/GatedCheckIn", Name = "User" )]
    public class User : SerializableBindableBase
    {
        private string myLogin;
        private string myEMail;
        [NonSerialized]
        private SecureString myPassword;

        [DataMember( Name = "Password" )]
        private byte[] mySerializablePassword;

        [DataMember]
        public string Login
        {
            get { return myLogin; }
            set { SetProperty( ref myLogin, value ); }
        }

        [DataMember]
        public string EMail
        {
            get { return myEMail; }
            set { SetProperty( ref myEMail, value ); }
        }

        public SecureString Password
        {
            get { return myPassword; }
            set
            {
                if( SetProperty( ref myPassword, value ) )
                {
                    // we do serialization as "update-on-write" because we also want to support cloning at any time
                    if( myPassword == null )
                    {
                        mySerializablePassword = null;
                    }
                    else
                    {
                        var bytes = Encoding.UTF8.GetBytes( myPassword.ToUnsecureString() );

                        mySerializablePassword = ProtectedData.Protect( bytes, null, DataProtectionScope.CurrentUser );
                    }
                }
            }
        }

        [OnDeserialized]
        private void OnDeserialized( StreamingContext context )
        {
            if( mySerializablePassword != null )
            {
                var bytes = ProtectedData.Unprotect( mySerializablePassword, null, DataProtectionScope.CurrentUser );

                myPassword = Encoding.UTF8.GetString( bytes ).ToSecureString();
            }
        }
    }
}
