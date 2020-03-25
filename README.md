# Quote-Retriever ![GitHub release](https://img.shields.io/github/release/stevewhit/quote-retriever.svg?color=green&style=popout)
Quote Retriever is a C# .NET desktop application for downloading and storing the latest End-Of-Day (EOD) market data for requested companies.

## Dependencies
There are two project dependencies to consider when installing & running this project. All three projects should reside in the same root folder in order to build properly.

1. [Framework.Generic](https://github.com/stevewhit/Framework.Generic)
1. [StockMarket.Generic](https://github.com/stevewhit/StockMarket.Generic)

## Installation
1. Create a free account with <a href="https://iexcloud.io">IEXCloud.io</a> and get a free API token.

2. Clone the repository and all [dependencies](#dependencies) to a single root folder.

```bash
git clone https://github.com/stevewhit/Quote-Retriever
git clone https://github.com/stevewhit/Framework.Generic
git clone https://github.com/stevewhit/StockMarket.Generic
```

3. Install NPM packages
```bash
npm install
```

4. Update application config file with your API token.
```config 
** QR.App -> App.config **

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

5. Follow installation steps for [StockMarket.Generic](https://github.com/stevewhit/StockMarket.Generic)

6. Follow installation steps for [Framework.Generic](https://github.com/stevewhit/Framework.Generic)

## Attribution to IEX
Stock market and company data for this application is [Powered by IEX Cloud](https://iexcloud.io).

View the [IEXCloud.io API](https://iexcloud.io/docs/api/#introduction).

## Lincense
Distributed under the MIT License. See [MIT License](https://choosealicense.com/licenses/mit/) for more information.
