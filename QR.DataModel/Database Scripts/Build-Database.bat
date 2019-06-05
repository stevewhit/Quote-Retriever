@echo OFF

SET "serverName=SQL-Server\DEVSQL"
SET "master_db_name=master"
SET "db_name=StockMarketData"
SET "db_user_name=DEVSQLAdmin"
SET "db_user_password=#EDC$RFV3edc4rfv"
	
:: Re-configure setup if this is a specific computer.
IF "%userdomain%\%username%" EQU "HYPERV\Administrator" (
	SET "serverName=DEV-SQL\ITSSQL"
	SET "master_db_name=master"
	SET "db_name=SMAData"
	SET "db_user_name=sa"
	SET "db_user_password=Pa$$w)rd123"
)

: SQLCMD command flags :
:: -S : Server\Instance name																		
:: -d : db_name
:: -U : login_id
:: -P : password
:: -i : input_file

echo.
echo ********************************
echo Running Initialization Scripts..
echo ********************************
echo.
:: Find and run all .sql scripts in the "Creation Scripts" directory that start with 0
CALL :RunSqlScriptsInDirectory "Creation Scripts" "%master_db_name%" "0*.sql"

echo -------------------------------------
echo.
echo **************************
echo Running Creation Scripts..
echo **************************
echo.
:: Find and run all .sql scripts in the "Creation Scripts" directory that don't start with 0
CALL :RunSqlScriptsInDirectory "Creation Scripts" %db_name% "*.sql" "0"

echo -------------------------------------
echo.
echo **************************
echo Running Delta Scripts..
echo **************************
echo.
:: Find and run all .sql scripts in the "Delta Scripts" directory that don't start with 0
CALL :RunSqlScriptsInDirectory "Delta Scripts" %db_name% "*.sql" "0"
goto end

:: Iterates through each of the files/folders recursively looking for sql scripts matching
:: the supplied format & runs eventually runs the sql command stored in that file.
: RunSqlScriptsInDirectory
SET temp_directory=%~1
SET temp_db_name=%~2
SET temp_filename_contains=%~3
SET temp_filename_doesnt_start_with=%~4

for /r "%temp_directory%" %%G in (%temp_filename_contains%) do ( 
	CALL :RunSqlScriptForFile "%temp_db_name%" "%temp_filename_doesnt_start_with%" "%%G" "%%~nxG" "%%~nG"
)
EXIT /B

: RunSqlScriptForFile
SET temp_db_name=%~1
SET temp_filename_doesnt_start_with=%~2
SET temp_full_filepath=%~3
SET temp_filename=%~4
SET temp_filepath=%~5

:: Iterate each of the supplied "doesn't start with" items
:: and determine if this file should be skipped
FOR %%a in (%temp_filename_doesnt_start_with%) do (
	if "%%a" NEQ "" (
		if /i "%temp_filename:~0,1%" EQU "%%a" (
			goto CONTINUE
)))

echo -------------------------------------
echo "%temp_filename%"
REM echo(
REM echo       Location: "%temp_filepath%"
echo(
SQLCMD -S %serverName% -d %temp_db_name% -U "%db_user_name%" -P "%db_user_password%" -i "%temp_full_filepath%" -h -1		

: CONTINUE
EXIT /B

:end
pause
@ECHO ON