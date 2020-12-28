using System;
using System.Collections.Generic;
using System.IO;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.Core
{
    public abstract class TechJournalOnTarget : ITechJournalOnTarget
    {
        #region Public Methods

        public virtual TechJournalPosition GetLastPosition()
        {
            throw new NotImplementedException();
        }

        public virtual int GetPortionSize()
        {
            throw new NotImplementedException();
        }

        public virtual void Save(EventData eventData)
        {
            throw new NotImplementedException();
        }

        public virtual void Save(IList<EventData> rowsData)
        {
            throw new NotImplementedException();
        }

        public virtual void SaveLogPosition(FileInfo logFileInfo, TechJournalPosition position)
        {
            throw new NotImplementedException();
        }

        public virtual void SetInformationSystem(TechJournalLogBase techJournalLog)
        {
            throw new NotImplementedException();
        }
        
        #endregion
    }
}
