﻿using System.Collections.Generic;
using System.IO;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant
{
    public interface ITechJournalOnTarget
    {
        TechJournalPosition GetLastPosition();
        void SaveLogPosition(FileInfo logFileInfo, TechJournalPosition position);
        int GetPortionSize();
        void SetInformationSystem(TechJournalLogBase techJournalLog);
        void Save(EventData eventData);
        void Save(IList<EventData> eventData);
    }
}