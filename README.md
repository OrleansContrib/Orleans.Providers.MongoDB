# Orleans.Providers.MongoDB
> Feedback would be appreciated.

A MongoDb implementation of the Orleans Providers. This includes the Membership (IMembershipTable & IGatewayListProvider), Reminder (IReminderTable), MongoStatisticsPublisher and IStorageProvider providers

## Usage
### Host Configuration


```ps
install-package Orleans.Providers.MongoDB
```
### Update OrleansConfiguration.xml in the Host application.
```json
{
  "marketComparisonTime": 120000,
  "arbitragePercentage": 15,
  "mail": {
    "active": true,
    "from": "ldevza@gmail.com",
    "to": "laredoza@gmail.com",
    "service": {
      "service": "gmail",
      "auth": {
        "user": "ldevza@gmail.com",
        "pass": "dolFLuvMA6zEiKdN7tRf"
      }
    }
  },
  "currencyConversion": {
    "options": {
      "host": "api.fixer.io",
      "path": "/latest?base=USD",
      "method": "GET"
    }
  },
  "markets": {
    "luno": {
      "options": {
        "host": "api.mybitx.com",
        "path": "/api/1/ticker?pair=XBTZAR",
        "method": "GET"
      }
    },
    "bitstamp": {
      "options": {
        "host": "www.bitstamp.net",
        "path": "/api/ticker",
        "method": "GET"
      }
    }
  }
}
```
## Todo

- Continue Refactor & add tests for storage
