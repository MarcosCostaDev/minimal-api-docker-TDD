# Minimal API with Docker and TDD

Github: [@MarcosCostaDev](https://github.com/MarcosCostaDev) | Twitter/X: [@MarcosCostaDev](https://twitter.com/MarcosCostaDev)

Project Url [https://github.com/MarcosCostaDev/minimal-api-docker-TDD](https://github.com/MarcosCostaDev/minimal-api-docker-TDD)

### Technologies
  - CSharp (C#)
  - .NET Core (.NET 7)
  - Swagger
  - xUnit (Test framework)
  - Docker / Docker compose (container orchestrator)
  - Redis (Distributed Cache)
  - Nginx (Load balancer)
  - Postgres (Database)

### Requirements
  - [Docker](https://docs.docker.com/engine/install/)

### How to run
  - Open the `./src` folder
  - Open a `terminal` / `bash` and execute `docker-compose up`
  - **API** will be available in the URL `http://localhost:9999`
  - **Swagger** will be available in the  URL `http://localhost:9999/swagger`
  - API Endpoints available
     - `GET /ping` health check, return `pong` if the API is up.
     - `POST /pessoas` for creating a record in the table `People` using the format message below
        ```json
        {
            "apelido" : "josé 12",
            "nome" : "José Roberto",
            "nascimento" : "2012-03-08T00:00:00",
            "stack" : ["C#", "Node", "Oracle"]
        }
        ``` 
     - `GET /pessoas/[id]` for retrieving a record of a person created using replacing `[id]` with the `person id`.
     - `GET /pessoas/t=[your search]` for retrieving records that contain the words informed, replace `[your search]` with your actual search.
     - `GET /contagem-pessoas` for retrieving the number of people created.

### Learn more

#### Running on Visual Studio

The project originally was created using Visual Studio 2022 Community Edition, but you can download its [most recent version](https://visualstudio.microsoft.com/vs/community/) and you need to download the [Visual Studio Container tools](https://learn.microsoft.com/en-us/visualstudio/containers/overview?view=vs-2022) for executing the project.

#### xUnit and integration tests

The integration test runs on docker containers that simulate this project in "Production".

#### Stress Test with the Postman

After you execute the application using `Docker-compose up` or run it using Visual Studio. You will be able to execute the stress tests for this API using Postman.

1. Download [Postman](https://www.postman.com/downloads/) and install it.
2. Go to import collection and select the file `./test/stress/RinhaBackEnd.postman_collection.json`
3. After importing the collection to the postman, right-click on the "RinhaBackEnd" collection, and select `Run Collection`, the `Runner Window` will be shown.
4. Right side, adjust the order of execution: place the `POST /pessoas` above the `GET /pessoas/:id` this will avoid trying to get a person that does not exist.
5. Left side of the Runner window, select `performance`, and use the configuration you want.
6. Click on run.

