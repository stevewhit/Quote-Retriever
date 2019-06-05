IF NOT EXISTS (SELECT * FROM Company WHERE Symbol='AAPL')
BEGIN
	PRINT 'Inserting "AAPL" into "Company" table..'

	INSERT INTO [dbo].[Company]
         	  ([Symbol]
          	 ,[Name])
     	VALUES
              ('AAPL', 'Apple Inc.')
END
ELSE
	PRINT '"AAPL" already exists in the "Company" table..'


