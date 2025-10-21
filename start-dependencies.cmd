@echo off
REM launches local SQL Server dependency so we can run from Visual Studio

set version="0.1.0"
set imageName="drawtogether.sqlserver"

if "%~1"=="" (
	REM No version tag specified
	echo No tag for [%imageName%] specified. Defaulting to [%version%]
) else (
	set version="%~1"
	echo Running [%imageName%] with tag [%~1]
)

docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1633:1433 --name "drawtogether-sql" -d "%imageName%:%version%"

if errorlevel 1 (
	echo "failed to start drawtogether.sqlserver - building image first then retrying"
	call %~dp0/infrastructure/build.all.cmd %version%
	if errorlevel 1 (
		echo "failed to build drawtogether.sqlserver - aborting"
		exit -1
	) else (
		docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=yourStrong(!)Password" -p 1633:1433 --name "drawtogether-sql" -d "%imageName%:%version%"
	)
)