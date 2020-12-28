using System.Collections.Generic;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant
{
    public sealed class BeforeExportDataEventArgs
    {
        public BeforeExportDataEventArgs()
        {
            Cancel = false;
        }

        public IReadOnlyList<EventData> Rows { set; get; }
        public bool Cancel { set; get; }
    }
}
