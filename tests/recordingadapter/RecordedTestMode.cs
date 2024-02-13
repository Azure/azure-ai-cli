using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public enum RecordedTestMode
    {
        Live,
        Record,
        Playback,
        // Backcompat with Track 1
        None = Live
    }
}
