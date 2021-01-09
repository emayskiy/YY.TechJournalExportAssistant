﻿using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using YY.TechJournalExportAssistant.ClickHouse;
using YY.TechJournalExportAssistant.Core;
using YY.TechJournalReaderAssistant;

namespace YY.TechJournalExportAssistantConsoleApp
{
    class Program
    {
        #region Private Static Member Variables

        private static long _totalRows;
        private static long _lastPortionRows;
        private static DateTime _beginPortionExport;
        private static DateTime _endPortionExport;

        #endregion

        #region Static Methods

        static void Main(string[] args)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            string connectionString = Configuration.GetConnectionString("TechJournalDatabase");

            IConfigurationSection techJournalSection = Configuration.GetSection("TechJournal");
            string techJournalPath = techJournalSection.GetValue("SourcePath", string.Empty);
            int watchPeriodSeconds = techJournalSection.GetValue("WatchPeriod", 60);
            int watchPeriodSecondsMs = watchPeriodSeconds * 1000;
            bool useWatchMode = techJournalSection.GetValue("UseWatchMode", false);
            int portion = techJournalSection.GetValue("Portion", 1000);
            string timeZoneName = techJournalSection.GetValue("TimeZone", string.Empty);

            TimeZoneInfo timeZone;
            if (string.IsNullOrEmpty(timeZoneName))
                timeZone = TimeZoneInfo.Local;
            else
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneName);

            IConfigurationSection techJournalLogSection = Configuration.GetSection("TechJournalLog");
            string techJournalName = techJournalLogSection.GetValue("Name", string.Empty);
            string techJournalDescription = techJournalLogSection.GetValue("Description", string.Empty);

            if (string.IsNullOrEmpty(techJournalPath))
            {
                Console.WriteLine("Не указан каталог с файлами данных технологического журнала.");
                Console.WriteLine("Для выхода нажмите любую клавишу...");
                Console.Read();
                return;
            }

            Console.WriteLine();
            Console.WriteLine();

            while (true)
            {
                TechJournalManager tjManager = new TechJournalManager(techJournalPath);
                foreach (var tjDirectory in tjManager.Directories)
                {
                    if (!tjDirectory.DirectoryData.Exists)
                        continue;

                    using (TechJournalExportMaster exporter = new TechJournalExportMaster())
                    {
                        exporter.SetTechJournalPath(tjDirectory.DirectoryData.FullName, timeZone);

                        TechJournalOnClickHouse target = new TechJournalOnClickHouse(connectionString, portion);
                        target.SetInformationSystem(new TechJournalLogBase()
                        {
                            Name = techJournalName,
                            DirectoryName = tjDirectory.DirectoryData.Name,
                            Description = techJournalDescription
                        });
                        exporter.SetTarget(target);

                        exporter.BeforeExportData += BeforeExportData;
                        exporter.AfterExportData += AfterExportData;
                        exporter.OnErrorExportData += OnErrorExportData;

                        _beginPortionExport = DateTime.Now;
                        while (exporter.NewDataAvailable())
                            exporter.SendData();
                    }
                }

                if (useWatchMode)
                {
                    if (Console.KeyAvailable)
                        if (Console.ReadKey().KeyChar == 'q')
                            break;
                    Thread.Sleep(watchPeriodSecondsMs);
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Для выхода нажмите любую клавишу...");
            Console.Read();
        }

        #endregion

        #region Events

        private static void BeforeExportData(BeforeExportDataEventArgs e)
        {
            _lastPortionRows = e.Rows.Count;
            _totalRows += e.Rows.Count;

            Console.SetCursorPosition(0, 0);
            Console.WriteLine("[{0}] Last read: {1}             ", DateTime.Now, e.Rows.Count);
        }
        private static void AfterExportData(AfterExportDataEventArgs e)
        {
            _endPortionExport = DateTime.Now;
            var duration = _endPortionExport - _beginPortionExport;

            Console.WriteLine("[{0}] Total read: {1}            ", DateTime.Now, _totalRows);
            Console.WriteLine("[{0}] {1} / {2} (sec.)           ", DateTime.Now, _lastPortionRows, duration.TotalSeconds);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Нажмите 'q' для завершения отслеживания изменений...");

            _beginPortionExport = DateTime.Now;
        }
        private static void OnErrorExportData(OnErrorExportDataEventArgs e)
        {
            Console.WriteLine(
                "Ошибка при экспорте данных." +
                "Критическая: {0}\n" +
                "\n" +
                "Содержимое события:\n" +
                "{1}" +
                "\n" +
                "Информация об ошибке:\n" +
                "\n" +
                "{2}",
                e.Critical, e.SourceData, e.Exception);
        }

        #endregion
    }
}
