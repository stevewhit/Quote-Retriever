IF NOT EXISTS (SELECT * FROM sys.tables WHERE NAME = 'Quotes')
BEGIN
	PRINT 'Creating table "Quotes"..'

	CREATE TABLE [dbo].[Quotes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[Date] [datetime] NOT NULL,
	[Open] [decimal](10, 2) NULL,
	[Close] [decimal](10, 2) NOT NULL,
	[High] [decimal](10, 2) NULL,
	[Low] [decimal](10, 2) NULL,
	[Volume] [bigint] NULL,
	[LastModifiedDate] [datetime] NULL 
	CONSTRAINT [PK_Quotes] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Quotes]  WITH CHECK ADD  CONSTRAINT [FK_Quotes_Companys] FOREIGN KEY([CompanyId]) REFERENCES [dbo].[Companys] ([Id])
	
	ALTER TABLE [dbo].[Quotes] CHECK CONSTRAINT [FK_Quotes_Companys]
END
ELSE
	PRINT 'The table "Quotes" already exists.'