IF NOT EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[TRG_Quotes_OnInsertOrUpdate]'))
BEGIN
	PRINT 'Creating trigger "TRG_Quotes_OnInsertOrUpdate"..'
	
	EXECUTE('CREATE TRIGGER TRG_Quotes_OnInsertOrUpdate ON [dbo].[Quotes] AFTER INSERT, UPDATE
			 AS
			 BEGIN
			 	 SET NOCOUNT ON;
			 	 UPDATE [dbo].[Quotes] SET LastModifiedDate=SYSDATETIME()
				 FROM dbo.Quotes INNER JOIN inserted
					 ON dbo.Quotes.Id = inserted.Id;
			 END');
END
ELSE
	PRINT 'The "TRG_Quotes_OnInsertOrUpdate" trigger already exists on the "Quotes" table.'