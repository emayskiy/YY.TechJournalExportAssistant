using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;
using ClickHouse.Client.Copy;
using YY.TechJournalExportAssistant.ClickHouse.Helpers;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalExportAssistant.Core.Helpers;
using YY.TechJournalReaderAssistant;
using YY.TechJournalReaderAssistant.Helpers;
using YY.TechJournalReaderAssistant.Models;

namespace YY.TechJournalExportAssistant.ClickHouse
{
    public class ClickHouseContext : IDisposable
    {
        #region Private Static Members

        #endregion

        #region Private Members

        private ClickHouseConnection _connection;
        private long logFileLastId = -1;

        #endregion

        #region Constructors

        public ClickHouseContext(string connectionSettings)
        {
            CheckDatabaseSettings(connectionSettings);

            _connection = new ClickHouseConnection(connectionSettings);
            _connection.Open();
            
            var cmdDDL = _connection.CreateCommand();

            cmdDDL.CommandText = Resource.Query_CreateTable_EventData;
            cmdDDL.ExecuteNonQuery();

            cmdDDL.CommandText = Resource.Query_CreateTable_LogFiles;
            cmdDDL.ExecuteNonQuery();
        }

        #endregion

        #region Public Methods

        #region RowsData

        public void SaveRowsData(TechJournalLogBase techJournalLog, List<EventData> eventData)
        {
            using (ClickHouseBulkCopy bulkCopyInterface = new ClickHouseBulkCopy(_connection)
            {
                DestinationTableName = "EventData",
                BatchSize = 100000
            })
            {
                var values = eventData.Select(i => new object[]
                {
                    techJournalLog.Name,
                    techJournalLog.DirectoryName,
                    i.Id,
                    i.Period,
                    i.Level,
                    i.Duration,
                    i.DurationSec,
                    i.EventName ?? string.Empty,
                    i.ServerContextName ?? string.Empty,
                    i.ProcessName ?? string.Empty,
                    i.SessionId ?? 0,
                    i.ApplicationName ?? string.Empty,
                    i.ClientId ?? 0,
                    i.ComputerName ?? string.Empty, 
                    i.ConnectionId ?? 0,
                    i.UserName ?? string.Empty,
                    i.ApplicationId ?? 0,
                    i.Context ?? string.Empty,
                    i.ActionType.GetDescription() ?? string.Empty,
                    i.Database ?? string.Empty,
                    i.DatabaseCopy ?? string.Empty,
                    i.DBMS.GetPresentation() ?? string.Empty,
                    i.DatabasePID ?? string.Empty,
                    i.PlanSQLText ?? string.Empty,
                    i.Rows ?? 0,
                    i.RowsAffected ?? 0,
                    i.SQLText ?? string.Empty,
                    i.SQLQueryOnly ?? string.Empty,
                    i.SQLQueryParametersOnly ?? string.Empty,
                    i.SQLQueryHash ?? string.Empty,
                    i.SDBL?? string.Empty,
                    i.Description?? string.Empty,
                    i.Message?? string.Empty,
                    i.GetCustomFieldsAsJSON() ?? string.Empty
                }).AsEnumerable();

                var bulkResult = bulkCopyInterface.WriteToServerAsync(values);
                bulkResult.Wait();
            }
        }
        public DateTime GetRowsDataMaxPeriod(TechJournalLogBase techJournalLog)
        {
            DateTime output = DateTime.MinValue;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT
                        MAX(Period) AS MaxPeriod
                    FROM EventData AS RD
                    WHERE TechJournalLog = {techJournalLog:String} ";
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "techJournalLog",
                    Value = techJournalLog.Name
                });
                using (var cmdReader = command.ExecuteReader())
                {
                    if (cmdReader.Read())
                        output = cmdReader.GetDateTime(0);
                }
            }

            return output;
        }
        public bool RowDataExistOnDatabase(TechJournalLogBase techJournalLog, EventData eventData)
        {
            bool output = false;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT
                        TechJournalLog,
                        Id,
                        Period
                    FROM EventData AS RD
                    WHERE TechJournalLog = {techJournalLog:String}
                        AND Id = {existId:Int64}
                        AND Period = {existPeriod:DateTime}";
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "techJournalLog",
                    DbType = DbType.AnsiString,
                    Value = techJournalLog.Name
                });
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "existId",
                    DbType = DbType.Int64,
                    Value = eventData.Id
                });
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "existPeriod",
                    DbType = DbType.DateTime,
                    Value = eventData.Period
                });

                using (var cmdReader = command.ExecuteReader())
                {
                    if (cmdReader.Read())
                        output = true;
                }
            }

            return output;
        }

        #endregion

        #region LogFiles

        public TechJournalPosition GetLogFilePosition(TechJournalLogBase techJournalLog)
        {
            var cmdGetLastLogFileInfo = _connection.CreateCommand();
            cmdGetLastLogFileInfo.CommandText =
                @"SELECT	                
	                LastEventNumber,
	                LastCurrentFileData,
	                LastStreamPosition
                FROM LogFiles AS LF
                WHERE TechJournalLog = {techJournalLog:String}
                    AND DirectoryName = {directoryName:String}
                    AND Id IN (
                        SELECT
                            MAX(Id) LastId
                        FROM LogFiles AS LF_LAST
                        WHERE LF_LAST.TechJournalLog = {techJournalLog:String}
                            AND LF_LAST.DirectoryName = {directoryName:String}
                    )";
            cmdGetLastLogFileInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "techJournalLog",
                DbType = DbType.AnsiString,
                Value = techJournalLog.Name
            });
            cmdGetLastLogFileInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "directoryName",
                DbType = DbType.AnsiString,
                Value = techJournalLog.DirectoryName
            });

            TechJournalPosition output = null;
            using (var cmdReader = cmdGetLastLogFileInfo.ExecuteReader())
            {
                if (cmdReader.Read())
                {
                    string fileData = cmdReader.GetString(1)
                        .Replace("\\\\", "\\")
                        .FixNetworkPath();

                    output = new TechJournalPosition(
                        cmdReader.GetInt64(0),
                        fileData,
                        cmdReader.GetInt64(2));
                }
            }

            return output;
        }
        public void SaveLogPosition(TechJournalLogBase techJournalLog, FileInfo logFileInfo, TechJournalPosition position)
        {
            var commandAddLogInfo = _connection.CreateCommand();
            commandAddLogInfo.CommandText =
                @"INSERT INTO LogFiles (
                    TechJournalLog,
                    DirectoryName,
                    Id,
                    FileName,
                    CreateDate,
                    ModificationDate,
                    LastEventNumber,
                    LastCurrentFileData,
                    LastStreamPosition
                ) VALUES (
                    {isId:String},
                    {directoryName:String},
                    {newId:Int64},
                    {FileName:String},
                    {CreateDate:DateTime},
                    {ModificationDate:DateTime},
                    {LastEventNumber:Int64},
                    {LastCurrentFileData:String},
                    {LastStreamPosition:Int64}     
                )";

            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "directoryName",
                DbType = DbType.Int64,
                Value = techJournalLog.DirectoryName
            });
            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "isId",
                DbType = DbType.Int64,
                Value = techJournalLog.Name
            });
            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "newId",
                DbType = DbType.Int64,
                Value = GetLogFileInfoNewId(techJournalLog)
            });
            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "FileName",
                DbType = DbType.AnsiString,
                Value = logFileInfo.Name
            });
            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "CreateDate",
                DbType = DbType.DateTime,
                Value = logFileInfo.CreationTimeUtc
            });
            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "ModificationDate",
                DbType = DbType.DateTime,
                Value = logFileInfo.LastWriteTimeUtc
            });
            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "LastEventNumber",
                DbType = DbType.Int64,
                Value = position.EventNumber
            });
            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "LastCurrentFileData",
                DbType = DbType.AnsiString,
                Value = position.CurrentFileData.Replace("\\", "\\\\")
            });
            commandAddLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "LastStreamPosition",
                DbType = DbType.Int64,
                Value = position.StreamPosition ?? 0
            });

            commandAddLogInfo.ExecuteNonQuery();
        }
        public long GetLogFileInfoNewId(TechJournalLogBase techJournalLog)
        {
            long output = 0;

            if (logFileLastId < 0)
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText =
                        @"SELECT
                            MAX(Id)
                        FROM LogFiles
                        WHERE TechJournalLog = {techJournalLog:String}
                            AND DirectoryName = {directoryName:String}";
                    command.Parameters.Add(new ClickHouseDbParameter
                    {
                        ParameterName = "techJournalLog",
                        Value = techJournalLog.Name
                    });
                    command.Parameters.Add(new ClickHouseDbParameter
                    {
                        ParameterName = "directoryName",
                        Value = techJournalLog.DirectoryName
                    });
                    using (var cmdReader = command.ExecuteReader())
                    {
                        if (cmdReader.Read())
                            output = cmdReader.GetInt64(0);
                    }
                }
            }
            else
            {
                output = logFileLastId;
            }

            output += 1;
            logFileLastId = output;

            return output;
        }
        public void RemoveArchiveLogFileRecords(TechJournalLogBase techJournalLog)
        {
            var commandRemoveArchiveLogInfo = _connection.CreateCommand();
            commandRemoveArchiveLogInfo.CommandText =
                @"ALTER TABLE LogFiles DELETE
                WHERE TechJournalLog = {techJournalLog:String}
                    AND DirectoryName = {directoryName:String}
                    AND Id < (
                    SELECT MAX(Id) AS LastId
                    FROM LogFiles lf
                    WHERE TechJournalLog = {techJournalLog:String}
                        AND DirectoryName = {directoryName:String}
                )";
            commandRemoveArchiveLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "techJournalLog",
                DbType = DbType.AnsiString,
                Value = techJournalLog.Name
            });
            commandRemoveArchiveLogInfo.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "directoryName",
                DbType = DbType.AnsiString,
                Value = techJournalLog.DirectoryName
            });
            commandRemoveArchiveLogInfo.ExecuteNonQuery();
        }

        #endregion

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();

                _connection.Dispose();
                _connection = null;
            }
        }

        #endregion

        #region Private Methods

        private void CheckDatabaseSettings(string connectionSettings)
        {
            ClickHouseHelpers.CreateDatabaseIfNotExist(connectionSettings);
        }

        #endregion
    }
}
