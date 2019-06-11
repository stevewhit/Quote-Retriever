IF EXISTS (SELECT * FROM sys.procedures WHERE NAME = 'ApplicationLogSelect')
BEGIN
	PRINT 'The stored procedure "ApplicationLogSelect" already exists.'
	SET NOEXEC ON
END
ELSE
	PRINT 'Creating stored procedure "ApplicationLogSelect"..'
GO

CREATE PROC [dbo].[ApplicationLogSelect]
AS

SELECT [Id]
	,[Date]
	,[Thread]
	,[Level]
	,[Logger]
	,[Message]
	,[Exception]
	,[Location]
	,[UserId]
FROM [dbo].[ApplicationLogs] logEntry
ORDER BY logEntry.Date DESC
GO

SET NOEXEC OFF
