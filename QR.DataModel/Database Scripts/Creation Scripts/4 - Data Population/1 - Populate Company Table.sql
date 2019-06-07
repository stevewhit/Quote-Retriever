IF NOT EXISTS (SELECT * FROM Companys WHERE Symbol='AAPL')
BEGIN
	PRINT 'Inserting "AAPL" into "Companys" table..'

	INSERT INTO [dbo].[Companys]
			   ([RetrieveQuotesFlag]
			   ,[Symbol]
			   ,[Name]
			   ,[Exchange]
			   ,[Industry]
			   ,[Website]
			   ,[Description]
			   ,[Sector]
			   ,[Tags])
     	VALUES
              (1, 'AAPL', 'Apple Inc.', 'NASDAQ', 'Technology', 'https://www.apple.com', 'Apple sells phone and computer products.', 'Technology', 'AAPL,Apple,Apple Inc.,OSX,iPhone')
END
ELSE
	PRINT '"AAPL" already exists in the "Companys" table..'


