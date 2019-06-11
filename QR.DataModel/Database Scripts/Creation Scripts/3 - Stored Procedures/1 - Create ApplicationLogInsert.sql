IF EXISTS (SELECT * FROM sys.procedures WHERE NAME = 'ApplicationLogInsert')
BEGIN
	PRINT 'The stored procedure "ApplicationLogInsert" already exists.'
	SET NOEXEC ON
END
ELSE
	PRINT 'Creating stored procedure "ApplicationLogInsert"..'
GO

CREATE proc [dbo].[ApplicationLogInsert]
	@logDate DateTime
	,@thread nvarchar(255)
	,@logLevel nvarchar(50)
	,@logger nvarchar(255)
	,@message nvarchar(max)
	,@exception nvarchar(max)
	,@location nvarchar(255)
	,@userId nvarchar(255)
AS
  
INSERT INTO [dbo].[ApplicationLogs] ([Date],[Thread],[Level],[Logger],[Message],[Exception], [Location], [UserId] ) 
VALUES (@logDate, @thread, @logLevel, @logger, @message, @exception, @location, @userId)
GO

SET NOEXEC OFF	
