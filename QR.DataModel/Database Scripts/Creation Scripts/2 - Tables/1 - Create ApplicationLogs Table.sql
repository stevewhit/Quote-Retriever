IF NOT EXISTS (SELECT * FROM sys.tables WHERE NAME = 'ApplicationLogs')
BEGIN
	PRINT 'Creating table "ApplicationLogs"..'

	CREATE TABLE [dbo].[ApplicationLogs](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Date] [datetime] NULL,
		[Thread] [nvarchar](255) NULL,
		[Level] [nvarchar](50) NULL,
		[Logger] [nvarchar](255) NULL,
		[Message] [nvarchar](max) NULL,
		[Exception] [nvarchar](max) NULL,
		[Location] [nvarchar](255) NULL,
		[UserId] [nvarchar](255) NULL,
	 CONSTRAINT [PK_ApplicationLogs] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
ELSE
	PRINT 'The table "ApplicationLogs" already exists.'


