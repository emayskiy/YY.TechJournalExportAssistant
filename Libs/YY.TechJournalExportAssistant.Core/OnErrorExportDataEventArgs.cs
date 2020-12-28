using System;

namespace YY.TechJournalExportAssistant
{
    public class OnErrorExportDataEventArgs
    {
        public Exception Exception { get; set; }
        public string SourceData { get; set; }
        public bool Critical { get; set; }
    }
}
