IF NOT EXISTS (SELECT * FROM sys.tables WHERE NAME = 'Company')
BEGIN
	PRINT 'Creating table "Company"..'

	CREATE TABLE [dbo].[Company](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Symbol] [nvarchar](20) NOT NULL,
		[Name] [nvarchar](50) NOT NULL,
		[Exchange] [nvarchar](20) NULL,
		[Industry] [nvarchar](25) NULL,
		[Website] [nvarchar](50) NULL,
		[Description] [nvarchar](255) NULL,
		[Sector] [nvarchar](50) NULL,
		[Tags] [nvarchar](1000) NULL,
	 CONSTRAINT [PK_Company] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
END
ELSE
	PRINT 'The table "Company" already exists.'


