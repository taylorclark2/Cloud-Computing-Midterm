This project is a simple, cloud-based RESTful API designed to manage a collection of TV shows. This API provides a lightweight backend service with full CRUD (Create, Read, Update, Delete) functionality. There is also a basic authentication using an API Key. The core data entity is a ‘Show’ object consisting of an ID, Title, Showrunner, Genre, Release Year, Number of Seasons, and Distributor attributes. This code uses C# on the .NET platform and deployed in the Azure Function App.

## Setup Instructions:

## Prerequisites
* .NET 8 SDK
* Azure Functions Core Tools
* Visual Studio Code (with Azure Extensions)
* Azure Subscription (SQL Database & Key Vault resources created)

### 1. Clone the Repository
```bash
git clone [https://github.com/](https://github.com/)[YOUR-USERNAME]/[YOUR-REPO-NAME].git
cd [YOUR-REPO-NAME]
```

### 2. Create a file named local.settings.json in the root directory
Paste the following configuration:

{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "[YOUR_SQL_CONNECTION_STRING_FROM_AZURE]",
    "KeyVaultUrl": "https://[YOUR-KEY-VAULT-NAME].vault.azure.net/"
  }
}

### 3. Database Migration
Initialize the database table using Entity Framework Core:

Using Bash insert this command:
    dotnet ef database update

### 4. Run Locally
Start the function app

Using Bash insert this command:
    func start

### API Reference

| Method | Endpoint    | Description    |
| :---:   | :---: | :---: |
| GET | /api/shows   | Retrieves all TV shows from the database. |
| GET | /api/shows/{id} | Retrieves a single show by ID. |
| POST | /api/shows  | Creates a new TV show. (Requires Title).  |
| PUT | /api/shows/{id} | Updates an existing show (Partial updates supported). |
| DELETE | /api/shows/{id}  | Deletes a show from the database.  |
| PATCH | /api/shows/validate | Triggers batch validation logic (Sets IsOld if Year < 2005).

### Sample JSON Body (POST/PUT)
{
  "title": "Breaking Bad",
  "showRunner": "Vince Gilligan",
  "genre": "Crime Drama",
  "releaseYear": 2008,
  "numberOfSeasons": 5,
  "distributor": "AMC"
}


## Deployment

1. Deploy to Azure Function App via VS Code.

2. Add SqlConnectionString and KeyVaultUrl to the Environment Variables in the Azure Portal.

3. Enable System-Assigned Managed Identity for the Function App and grant it access to the Key Vault.


## Automation & Governance

* Logic App: Runs daily to trigger the /validate endpoint.

* Traceability: The database tracks a LastValidated timestamp for every record.

* Monitoring: Live metrics available via the Custom Azure Dashboard.