IF NOT EXISTS (SELECT * FROM sys.tables WHERE NAME = 'Companys')
BEGIN
	PRINT 'Creating table "Companys"..'

	CREATE TABLE [dbo].[Companys](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[RetrieveQuotesFlag] [bit] NOT NULL DEFAULT(1),
		[Symbol] [nvarchar](20) NOT NULL,
		[Name] [nvarchar](50) NOT NULL,
		[Exchange] [nvarchar](20) NULL,
		[Industry] [nvarchar](25) NULL,
		[Website] [nvarchar](50) NULL,
		[Description] [nvarchar](255) NULL,
		[Sector] [nvarchar](50) NULL,
		[Tags] [nvarchar](1000) NULL,
	 CONSTRAINT [PK_Companys] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
END
ELSE
	PRINT 'The table "Companys" already exists.'


