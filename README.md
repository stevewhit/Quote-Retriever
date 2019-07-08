# Quote-Retriever
Quote Retriever is a C# .NET desktop application for downloading and storing the latest End-Of-Day (EOD) market data for requested companies.

## Installation
1. Create a free account with <a href="https://iexcloud.io">IEXCloud.io</a> and get a free API token.

2. Clone the repository and all dependencies

''' bash
git clone https://github.com/stevewhit/Quote-Retriever
git clone https://github.com/stevewhit/Framework.Generic
git clone https://github.com/stevewhit/StockMarket.Generic
'''

3. Install NPM packages
''' bash
npm install
'''

4. Update application config file with API token.
''' Git Config 
<!-- Remove -->
<connectionStrings configSource="secretConnectionStrings.config" />

<!-- Add -->
<connectionStrings>
	<appSettings>
	  <add key="IEXCloudToken" value="[YOUR_API_TOKEN_HERE]" />
	  <add key="IEXCloudTokenTest" value="[YOUR_TEST_API_TOKEN_HERE]"/>
	</appSettings>
<connectionStrings/>
'''

## Attribution to IEX
<a href="https://iexcloud.io">Powered by IEX Cloud</a>

For quote retrieval, the <a href="https://iexcloud.io">IEXCloud.io API</a> is used.

Attribution is required of all users of iexcloud. Put “Powered by IEX Cloud” somewhere on your site or app, and link that text to https://iexcloud.io. Alternately, the attribution link can be included in your terms of service.

### IEXCloud.io API
https://iexcloud.io/docs/api/#introduction

## Lincense
Distributed under the MIT License. See [MIT License](https://choosealicense.com/licenses/mit/) for more information.