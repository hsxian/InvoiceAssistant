@echo off
set DIR_OUTPUT=InvoiceAssistant-publish-win
set DIR_OUTPUT_TEMP=InvoiceAssistant-publish-win-temp

taskkill /IM InvoiceAssistant.exe /F >nul 2>&1
ping -n 2 127.0.0.1 >nul

if exist "%DIR_OUTPUT_TEMP%" (
    if exist "%DIR_OUTPUT%" (
        rmdir /s /q "%DIR_OUTPUT%"
        echo delete %DIR_OUTPUT%
    )
    move /y "%DIR_OUTPUT_TEMP%" "%DIR_OUTPUT%"
    echo move %DIR_OUTPUT_TEMP% to %DIR_OUTPUT%
)

dotnet publish ./InvoiceAssistant.Console/InvoiceAssistant.Console.csproj -r win-x64 --self-contained true /p:PublishSingleFile=true -c realse -o %DIR_OUTPUT%
