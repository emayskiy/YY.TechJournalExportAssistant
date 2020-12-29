# Помощник экспорта технологического журнала

| Nuget-пакет | Актуальная версия | Описание |
| ----------- | ----------------- | -------- |
| YY.TechJournalExportAssistant.Core | [![NuGet version](https://badge.fury.io/nu/YY.TechJournalExportAssistant.Core.svg)](https://badge.fury.io/nu/YY.TechJournalExportAssistant.Core) | Базовый пакет |
| YY.TechJournalExportAssistant.ClickHouse | [![NuGet version](https://badge.fury.io/nu/YY.TechJournalExportAssistant.ClickHouse.svg)](https://badge.fury.io/nu/YY.TechJournalExportAssistant.ClickHouse) | Пакет для экспорта в базу ClickHouse |

Решение для экспорта данных технологического журнала платформы 1С:Предприятие 8.x в нестандартные хранилища данных. С помощью библиотеки **[YY.TechJournalReaderAssistant](https://github.com/YPermitin/YY.TechJournalExportAssistant)** реализовано чтение данных из файлов лога технологического журнала (*.log).

Последние новости об этой и других разработках, а также выходе других материалов, **[смотрите в Telegram-канале](https://t.me/DevQuietPlace)**.

### Состояние сборки
| Windows |  Linux |
|:-------:|:------:|
| [![Build status](https://ci.appveyor.com/api/projects/status/vjvb80jf0p5m18vh?svg=true)](https://ci.appveyor.com/project/YPermitin/yy-techjournalexportassistant) | ![.NET](https://github.com/YPermitin/YY.TechJournalExportAssistant/workflows/.NET/badge.svg) |

### Code Climate

[![Maintainability](https://api.codeclimate.com/v1/badges/30500457be8c7e4f1562/maintainability)](https://codeclimate.com/github/YPermitin/YY.TechJournalExportAssistant/maintainability)

## Благодарности

Выражаю большую благодарность **[Алексею Бочкову](https://github.com/alekseybochkov)** как идейному вдохновителю. 

Именно его разработка была первой реализацией чтения и экспорта технологического журнала 1С - **[TJ_LOADER](https://github.com/alekseybochkov/tj_loader)**. Основную идею и некоторые примеры реализации взял именно из нее, но с полной переработкой архитектуры библиотеки.

## Состав репозитория

* Библиотеки
  * YY.TechJournalExportAssistant.Core - ядро библиотеки с основным функционалом чтения и передачи данных.
  * YY.TechJournalExportAssistant.ClickHouse - функционал для экспорта данных в базу ClickHouse.
* Примеры приложений
  * YY.YY.TechJournalExportAssistantConsoleApp - пример приложения для экспорта данных в базу ClickHouse.

## Требования и совместимость

Работа библиотеки тестировалась со следующими версиями компонентов:

* Платформа 1С:Предприятие версии от 8.3.6 и выше.
* ClickHouse 20.9 и выше.

В большинстве случаев работоспособность подтверждается и на более старых версиях ПО, но меньше тестируется. Основная разработка ведется для Microsoft Windows, но некоторый функционал проверялся под *.nix.*

## Пример использования

Репозиторий содержим пример консольного приложения для экспорта данных в базу ClickHouse - **YY.YY.TechJournalExportAssistantConsoleApp**.

### Конфигурация

Первое, с чего следует начать - это конфигурационный файл приложения "appsettings.json". Это JSON-файл со строкой подключения к базе данных, сведениями об технологическом журнале и параметрами его обработки. Располагается в корне каталога приложения.

```json
{
  "ConnectionStrings": {
    "TechJournalDatabase": "Host=127.0.0.1;Port=8123;Username=default;password=;Database=AmazingTechJournalDatabase;"
  },
  "TechJournalLog": {
    "Name": "AmazingTechJournalDatabase",
    "Description": "Технологический журнал. Очень разный."
  },
  "TechJournal": {
    "SourcePath": "C:\\TechJournalDirectory",
    "UseWatchMode": true,
    "WatchPeriod": 5,
    "Portion": 10000
  }
}
```

Секция **"ConnectionStrings"** содержит строку подключения **"TechJournalDatabase"** к базе данных для экспорта. База будет создана автоматически при первом запуске приложения. Также можно создать ее вручную, главное, чтобы структура была соответствующей. Имя строки подключения **"TechJournalDatabase"** - это значение по умолчанию. Контекст приложения будет использовать ее автоматически, если это не переопределено разработчиком явно.

Секция **"TechJournalLog"** содержит название для текущего журнала и ее описание, ведь вариантов конфигурации сбора технологического журнала может быть бесконечное количество. Эта настройка позволяет их разделять при хранении в одной базе.

Секция **"TechJournal"** содержит параметры обработки технологического журнала:

* **SourcePath** - путь к каталогу с файлами технологического журнала. Необходимо указывать каталог аналогично тому, как он был указан в файле настройки технологического журнала (т.е. в нем должны быть каталоги по процессам и т.д.).
* **UseWatchMode** - при значении false приложение завершит свою работу после загрузки всех данных. При значении true будет отслеживать появления новых данных пока приложение не будет явно закрыто.
* **WatchPeriod** - период в секундах, с которым приложение будет проверять наличие изменений. Используется, если параметр "UseWatchMode" установлен в true.
* **Portion** - количество записей, передаваемых в одной порции в хранилище.

Настройки "UseWatchMode" и "WatchPeriod" не относятся к библиотеке. Эти параметры добавлены лишь для примера консольного приложения и используется в нем же.

### Пример использования

На следующем листинге показан пример использования библиотеки.

```csharp
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

        IConfigurationSection eventLogSection = Configuration.GetSection("TechJournal");
        string techJournalPath = eventLogSection.GetValue("SourcePath", string.Empty);
        int watchPeriodSeconds = eventLogSection.GetValue("WatchPeriod", 60);
        int watchPeriodSecondsMs = watchPeriodSeconds * 1000;
        bool useWatchMode = eventLogSection.GetValue("UseWatchMode", false);
        int portion = eventLogSection.GetValue("Portion", 1000);

        IConfigurationSection techJournalSection = Configuration.GetSection("TechJournalLog");
        string techJournalName = techJournalSection.GetValue("Name", string.Empty);
        string techJournalDescription = techJournalSection.GetValue("Description", string.Empty);

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
                    exporter.SetTechJournalPath(tjDirectory.DirectoryData.FullName);

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
}
```

Так выглядят примеры обработчиков событий.

```csharp
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
```

С их помощью можно проанализировать какие данные будут выгружены и отказаться от выгрузки с помощью поля "Cancel" в параметре события "BeforeExportDataEventArgs" в событии "Перед экспортом данных". В событии "После экспорта данных" можно проанализировать выгруженные данные.

## Cценарии использования

Библиотека может быть использования для создания приложений для экспорта технологического журнала платформы 1С:Предприяние 8.ч в нестандартные хранилища, которые упрощают анализ данных и позволяют организовать эффективный мониторинг.

## TODO

Планы в части разработки:

* Добавить возможность экспорта данных в PostgreSQL
* Улучшить обработку ошибок по уровням возникновения (критические и нет)
* Улучшение производительности и добавление bencmark'ов
* Расширение unit-тестов библиотеки

## Лицензия

MIT - делайте все, что посчитаете нужным. Никакой гарантии и никаких ограничений по использованию.
