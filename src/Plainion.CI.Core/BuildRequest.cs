using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Plainion.CI
{
    [Serializable]
    [DataContract( Namespace = "http://github.com/ronin4net/plainion/GatedCheckIn", Name = "BuildRequest" )]
    public class BuildRequest
    {
        [DataMember]
        public string CheckInComment { get; set; }

        [DataMember]
        public string[] FilesExcludedFromCheckIn { get; set; }
    }
}
