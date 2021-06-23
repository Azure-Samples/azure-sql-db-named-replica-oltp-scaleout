# Azure SQL Database Hyperscale Named Replicas OLTP Scale-Out Sample

This sample shows how you can use Azure SQL Database Hyperscale Named Replicas to easily scale-out an OLTP solution.

## Scenario

The code in the `./app` folder provide a REST endpoint that implements a basic shopping cart API. The REST endpoint has three methods:

- `GET /{id}`: return a JSON document containing a user shopping cart
- `GET /package/{id}`: return all the shopping carts that contains a package with the specified `id` value
- `PUT /`: store the received JSON document containing a user shopping cart

### Shopping Cart

The shopping cart is a JSON document, generated randomly, with this schema:

```json
{
    "cart_id": <cart_id>,
    "user_id": <user_id>,
    "items":[{
        "id": <item_id>,
        "quantity": <quantity>,
        "price": <price>,
        "details": {
            <random attributes>,
            "package": {    
                "id": <package_id>
            }
        }
    }]
}
```

The number of items is random and can be up to 10. The details of each item in the shopping cart are also randomly generated. The `package` object is also randomly added to some item.
Here's an example of a generated shopping cart JSON document:

```json
{
    "cart_id": 17,
    "user_id": 34851,
    "items": [,
         {
            "id": 5306,
            "quantity": 8,
            "price": 683.7700,
            "details": {
                "scyOQ": "kkJaPOdmwvQFvLEDNhXCACjBMRKOVwgvxoCHMqCORMRgZTLOkBLcRaq",
                "troG": "gSoTExi",
                "fVWdI": "zqCrSZUaPBMWxAALvAcnBHIXxesnQHIUOYkBWIpITfFLpJAlcZorPDRXZUihHRrSHuLjvGJKQWgUuMpZXr"
            }
        }        
        {
            "id": 7884,
            "quantity": 10,
            "price": 199.9600,
            "details": {
                "Mtjf": "EOgJjIlOkjWfEQpUePUwFyFxttnjKpZKwqCiYAwzCDnLyKLvfYOMpsFSprQdpwsSeCIbQYOOyaCUnu",
                "IJUybP": "jLdRhFzZuNkHDxmTQGovxAbtNQQNbSVdEBvsptWWjRihAsGzBRpCVJhvDkalCOwpwtyzEZRwdHzbRmBfzZmsMQYRzrPFY",
                "AiDiQ": "ZRCnVgq",
                "UdJSfHF": "uRGSVQcgVVpimfgLbfhOhIttoXsVdCdDBLPzfoBMYEuetJsPumtxzesBakwVvTWlMRpmVEHbTxCtuSzjTKdAlvY",
                "erUs": "pXDU",
                "tsssjH": "hESyhXmcfECkZ",
                "wHAGD": "QUbgrLxTXhbsClSgdBoTBlKbVcGGpW",
                "uJYQn": "HUNpGJWLnuUSZZBosldMqqWdeg",
                "package": {
                    "id": 3064
                }
            }
        }
    ]
}
```

### Database

Received JSON document is saved into the `dbo.shopping_cart` table. The well-know elements are saved into proper relational columns to have the best performance possible, while the item details, being completely dynamic, are stored as a JSON document.

The scripts to create the database and to created the required objects are available in the `./app/Database` folder.

## Deploy the app

Use the script `./app/azure-deploy.sh` to deploy the REST API in Azure. You can use WSL2 (script has been tested on WSL2 Ubuntu 20) or the Azure Cloud Shell.

## Create some workload

To simulate a typical shopping cart activity, where new shopping cart are created and retrieved, the open source load testing tool [Locust](https://locust.io) is used. The test solution is available in the `./test` folder and the load test script is `./test/locust/locustfile.py`.

Locust can be run locally or via Docker. Even better, can be run directly on Azure via Azure Container Instances, to avoid any potential network or resource bottleneck that can be found when running on local environments.

To deploy and run Locust in Azure, use the `./test/azure-deploy.sh` script.
## Scalability Challenges

Coming soon....