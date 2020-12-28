namespace YY.TechJournalExportAssistant.Core
{
    public class TechJournalLogBase
    {
        public virtual string Name { get; set; }
        public virtual string DirectoryName { get; set; }
        public virtual string Description { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
