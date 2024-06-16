# Price Service #

A service for processing price, storing price, and applying price rules.

## How to run projects ##

.NET 3 on Mac OS has issue with HTTP/2 over TLS. Some projects needs extra configuration.

Before we proceed, please acquire the keys required to connect Kafka and put it in the `DevKeys` directory.
You will need 3 files: keystore.key, truststore.cer.pem, and, keystore.crt.


### Scheduler project ###

There are two services required to run this project locally. 

| Service                        | Comment                              |
|--------------------------------|--------------------------------------|
| `MongoDb`                      | Expose port 27017                    |
| `Redis`                        | Expose port 6379                     |

Docker command to run MongoDb and Redis
    docker run --name ps-mongo -p 27017:27017 -d mongo
    docker run --name ps-redis -p 6379:6379 -d redis


### API project ###


There are three services required to run this project locally. 

| Service                        | Comment                              |
|--------------------------------|--------------------------------------|
| `MongoDb`                      | Expose port 27017                    |
| `Redis`                        | Expose port 6379                     |
| `Localstack`                   | Expose port 6379                     |

Docker command to run MongoDb and Redis
    docker run --name ps-mongo -p 27017:27017 -d mongo
    docker run --name ps-redis -p 6379:6379 -d redis

Localstack command. Requires a configured AWS CLI (Ref: https://medium.com/@shtanko.michael/mocking-aws-with-localstack-in-net-core-3-ef32ae888706)
    docker-compose -f docker-compose-localstack.yml up -d
    aws --endpoint-url=http://localhost:4566 s3 mb s3://price-service

### Admin project ###

Admin project also uses `IsMacOs` environment variable as mentioned above.  Also, additional settings are needed in `appsettings.json`.
Please ensure that app settings are set properly for each environment settings.

### add user manual for Admin project ###

database user storage in `MongoDb` Document `User`

* `Data structure`

`{
    "_id" : "ph*********@c*****l.t***",
    "Role" : "Admin",
    "LastUpdate" : ISODate("2020-06-22T10:49:45.762Z")
}`


## Actor structure ##

### API ###

* `api` supervisor
    * `price-keeper` local price cache for API
        * `other-keepers` *group* of scheduler keepers in the cluster

### Scheduler ###

* `heralds` local herald *group* within a scheduler
* `schedule-organizer` Polls the schedule collection for price updates
* `price-heralds` *router* to heralds to schedulers in the cluster.
* `price-importer` monitor Kafka queue, validate inputs, and pass messages to heralds
* `price-keeper` updates cache once a price is published. It receives the final `PriceModel` from a herald. 
    * `broadcaster` *group* of `price-keeper` in the cluster (excluding itself). 
