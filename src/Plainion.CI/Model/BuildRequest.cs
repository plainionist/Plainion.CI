using System;
using System.Collections.Generic;

namespace Plainion.CI.Model
{
    [Serializable]
    class BuildRequest
    {
        public string CheckInComment { get; set; }

        public IReadOnlyCollection<string> Files { get; set; }
    }
}
