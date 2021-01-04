using System;

namespace YY.TechJournalExportAssistant.Core
{
    public interface ITechJournalExportMaster
    {
        void SetTechJournalPath(string eventLogPath, TimeZoneInfo timeZone);
        bool NewDataAvailable();
        void SendData();
        TimeZoneInfo GetTimeZone();
    }
}
