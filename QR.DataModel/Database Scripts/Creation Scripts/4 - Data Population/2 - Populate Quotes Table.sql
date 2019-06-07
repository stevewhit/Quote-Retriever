IF NOT EXISTS (SELECT * FROM Quotes q WHERE CompanyId=1 AND Date='2099-01-01 11:11:11.111')
BEGIN
	PRINT 'Inserting Quote1 into "Quotes" table..'

	INSERT INTO [dbo].[Quotes]
			   ([CompanyId]
			   ,[Date]
			   ,[Close])
     	VALUES
              (1, '2099-01-01 11:11:11.111', 111.11)
END
ELSE
	PRINT 'Quote1 already exists in the "Quotes" table..'


