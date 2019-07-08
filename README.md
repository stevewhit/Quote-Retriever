# Quote-Retriever
Quote Retriever is a C# .NET desktop application for downloading and storing the latest End-Of-Day (EOD) market data for requested companies.

## Installation
1. Create a free account with <a href="https://iexcloud.io">IEXCloud.io</a> and get a free API token.

2. Clone the repository and all dependencies

```bash
git clone https://github.com/stevewhit/Quote-Retriever
git clone https://github.com/stevewhit/Framework.Generic
git clone https://github.com/stevewhit/StockMarket.Generic
```

3. Install NPM packages
```bash
npm install
```

4. Update application config file with API token.
```config 
<!-- Remove -->
<connectionStrings configSource="secretConnectionStrings.config" />

<!-- Add -->
<connectionStrings>
  <appSettings>
    <add key="IEXCloudToken" value="[YOUR_API_TOKEN_HERE]" />
    <add key="IEXCloudTokenTest" value="[YOUR_TEST_API_TOKEN_HERE]"/>
  </appSettings>
<connectionStrings/>
```

## Attribution to IEX
For EOD stock data, this application is [Powered by IEX Cloud](https://iexcloud.io).

View the [IEXCloud.io API](https://iexcloud.io/docs/api/#introduction).

## Lincense
Distributed under the MIT License. See [MIT License](https://choosealicense.com/licenses/mit/) for more information.