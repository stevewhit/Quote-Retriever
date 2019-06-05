IF NOT EXISTS (SELECT * FROM master.dbo.syslogins WHERE [name] = 'StockMarketDataAdmin')
BEGIN
	PRINT 'Creating "StockMarketDataAdmin" login..'

	CREATE LOGIN [StockMarketDataAdmin] WITH PASSWORD='@WSX#EDC2wsx3edc', DEFAULT_LANGUAGE=[us_english], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF
	ALTER SERVER ROLE [sysadmin] ADD MEMBER [StockMarketDataAdmin]
END
ELSE
	PRINT 'The user "StockMarketDataAdmin" login already exists..'
