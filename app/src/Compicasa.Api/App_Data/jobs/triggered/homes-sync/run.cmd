@echo off
rem The daily homes crawl (D-035, D-036). Runs the app in crawl mode with the web app's own settings and
rem managed identity. The schedule is in settings.job, in Madrid time (WEBSITE_TIME_ZONE).
cd /d "%HOME%\site\wwwroot"
dotnet Compicasa.Api.dll crawl tecnocasa --Crawl:Max=3000
