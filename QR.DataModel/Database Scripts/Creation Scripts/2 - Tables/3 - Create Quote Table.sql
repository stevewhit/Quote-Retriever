IF NOT EXISTS (SELECT * FROM sys.tables WHERE NAME = 'Quote')
BEGIN
	PRINT 'Creating table "Quote"..'

	CREATE TABLE [dbo].[Quote](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[Date] [datetime] NOT NULL,
	[Open] [decimal](10, 2) NULL,
	[Close] [decimal](10, 2) NOT NULL,
	[High] [decimal](10, 2) NULL,
	[Low] [decimal](10, 2) NULL,
	[Volume] [bigint] NULL,
	CONSTRAINT [PK_Quote] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Quote]  WITH CHECK ADD  CONSTRAINT [FK_Quote_Company] FOREIGN KEY([CompanyId]) REFERENCES [dbo].[Company] ([Id])
	
	ALTER TABLE [dbo].[Quote] CHECK CONSTRAINT [FK_Quote_Company]
END
ELSE
	PRINT 'The table "Quote" already exists.'


