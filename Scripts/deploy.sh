#!/bin/sh
API_KEY = $1

dotnet nuget push /Libs/YY.TechJournalExportAssistant.Core/bin/Release/YY.TechJournalExportAssistant.Core.*.nupkg -k $1 -s https://api.nuget.org/v3/index.json --skip-duplicate

dotnet nuget push /Libs/YY.TechJournalExportAssistant.ClickHouse/bin/Release/YY.TechJournalExportAssistant.ClickHouse.*.nupkg -k $1 -s https://api.nuget.org/v3/index.json --skip-duplicate