IF NOT EXISTS (SELECT * FROM sys.sysusers WHERE [name] = 'StockMarketDataAdmin')
BEGIN
	PRINT 'Creating "StockMarketDataAdmin" user"..'

	CREATE USER [StockMarketDataAdmin] FOR LOGIN [StockMarketDataAdmin] WITH DEFAULT_SCHEMA=[dbo]
	GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE TO [StockMarketDataAdmin]
END
ELSE
	PRINT 'The user "StockMarketDataAdmin" already exists..'
