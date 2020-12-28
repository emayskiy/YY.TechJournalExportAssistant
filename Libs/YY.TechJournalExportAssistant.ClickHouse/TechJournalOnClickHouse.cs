using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalReaderAssistant;
using EventData = YY.TechJournalReaderAssistant.Models.EventData;

namespace YY.TechJournalExportAssistant.ClickHouse
{
    public class TechJournalOnClickHouse : TechJournalOnTarget
    {
        #region Private Member Variables

        private const int _defaultPortion = 1000;
        private readonly int _portion;
        private DateTime _maxPeriodRowData;
        private TechJournalLogBase _techJournalLog;
        private readonly string _connectionString;
        private TechJournalPosition _lastTechJournalFilePosition;
        private int _stepsToClearLogFiles = 1000;
        private int _currentStepToClearLogFiles;

        #endregion

        #region Constructor

        public TechJournalOnClickHouse() : this(null, _defaultPortion)
        {

        }
        public TechJournalOnClickHouse(int portion) : this(null, portion)
        {
            _portion = portion;
        }
        public TechJournalOnClickHouse(string connectionString, int portion)
        {
            _portion = portion;
            _maxPeriodRowData = DateTime.MinValue;

            if (connectionString == null)
            {
                IConfiguration Configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();
                _connectionString = Configuration.GetConnectionString("TechJournalDatabase");
            }

            _connectionString = connectionString;
        }

        #endregion

        #region Public Methods

        public override TechJournalPosition GetLastPosition()
        {
            if (_lastTechJournalFilePosition != null)
                return _lastTechJournalFilePosition;

            TechJournalPosition position;
            using(var context = new ClickHouseContext(_connectionString))
                position = context.GetLogFilePosition(_techJournalLog);
            
            _lastTechJournalFilePosition = position;
            return position;
        }
        public override void SaveLogPosition(FileInfo logFileInfo, TechJournalPosition position)
        {
            using (var context = new ClickHouseContext(_connectionString))
            {
                context.SaveLogPosition(_techJournalLog, logFileInfo, position);
                if (_currentStepToClearLogFiles == 0 || _currentStepToClearLogFiles >= _stepsToClearLogFiles)
                {
                    context.RemoveArchiveLogFileRecords(_techJournalLog);
                    _currentStepToClearLogFiles = 0;
                }
                _currentStepToClearLogFiles += 1;
            }

            _lastTechJournalFilePosition = position;
        }
        public override int GetPortionSize()
        {
            return _portion;
        }
        public override void Save(EventData rowData)
        {
            IList<EventData> rowsData = new List<EventData>
            {
                rowData
            };
            Save(rowsData);
        }

        public override void Save(IList<EventData> rowsData)
        {
            using (var context = new ClickHouseContext(_connectionString))
            {
                if (_maxPeriodRowData == DateTime.MinValue)
                    _maxPeriodRowData = context.GetRowsDataMaxPeriod(_techJournalLog);

                List<EventData> newEntities = new List<EventData>();
                foreach (var itemRow in rowsData)
                {
                    if (itemRow == null)
                        continue;
                    if (_maxPeriodRowData != DateTime.MinValue && itemRow.Period <= _maxPeriodRowData)
                        if (context.RowDataExistOnDatabase(_techJournalLog, itemRow))
                            continue;

                    newEntities.Add(itemRow);
                }
                context.SaveRowsData(_techJournalLog, newEntities);
            }
        }
        public override void SetInformationSystem(TechJournalLogBase techJournalLog)
        {
            _techJournalLog = techJournalLog;
        }

        #endregion
    }
}
