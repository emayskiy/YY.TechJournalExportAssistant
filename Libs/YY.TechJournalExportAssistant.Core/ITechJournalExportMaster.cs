namespace YY.TechJournalExportAssistant.Core
{
    public interface ITechJournalExportMaster
    {
        void SetTechJournalPath(string eventLogPath);
        bool NewDataAvailable();
        void SendData();
    }
}
