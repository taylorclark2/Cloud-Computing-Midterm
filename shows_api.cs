using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Midterm.Project;

public class shows_api
{
    private readonly ILogger<shows_api> _logger;
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly TelemetryClient _telemetry;
    public shows_api(ILogger<shows_api> logger, IConfiguration config, AppDbContext context, TelemetryClient telemetry)
    {
        _logger = logger;
        _config = config;
        _context = context;
        _telemetry = telemetry;
    }

    public class Show
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ShowRunner { get; set; }
        public string? Genre { get; set; }
        public int ReleaseYear { get; set; }
        public int NumberOfSeasons { get; set; }
        public string? Distributor { get; set; }
        public bool IsOld { get; set; }
        public DateTime? LastValidated { get; set; }
    }

    [Function("GetShows")] //Returns shows
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shows")] HttpRequest req)
    {
        //Fetch the key from Key Vault
        string validApiKey = await GetSecretKeyFromVault();

        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != validApiKey)
        {
            return new UnauthorizedResult(); //Returns error
        }
        var showList = await _context.Shows.ToListAsync();
        return new OkObjectResult(showList);
    }

    [Function("GetShowById")]
    public async Task<IActionResult> GetById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shows/{id}")] HttpRequest req, int id)
    {   
        //Fetch the key from Key Vault
        string validApiKey = await GetSecretKeyFromVault();

        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != validApiKey)
            return new UnauthorizedResult(); //Returns error

        //Check if the provided ID is a positive number
        if (id <= 0)
            return new BadRequestObjectResult("The show ID must be a positive number");
        var show = await _context.Shows.FindAsync(id);

        //If no show is found with that ID, return a 404 Not Found error
        if (show == null)
            return new NotFoundResult();

        //If the show is found, return it
        return new OkObjectResult(show);
    }

    [Function("CreateShow")] //Creates a show
    public async Task<IActionResult> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "shows")] HttpRequest req)
    {
        //Fetch the key from Key Vault
        string validApiKey = await GetSecretKeyFromVault();

        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != validApiKey)
        {
            return new UnauthorizedResult(); //Returns error
        }
        //Takes JSON data and makes into a variable
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        try
        {
        var newShow = JsonSerializer.Deserialize<Show>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //Checks if newShow variable is valid 
        if (newShow == null)
            return new BadRequestObjectResult("Bad Request");

        if (string.IsNullOrWhiteSpace(newShow.Title))
            return new BadRequestObjectResult("The 'Title' field is required");

        _context.Shows.Add(newShow);
        await _context.SaveChangesAsync();
        return new CreatedResult($"/api/shows/{newShow.Id}", newShow);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult("Invalid JSON format");
        }
    }


    [Function("DeleteShow")] //Deletes a show
    public async Task<IActionResult> Delete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "shows/{id}")] HttpRequest req, int id)
    {
        //Fetch the key from Key Vault
        string validApiKey = await GetSecretKeyFromVault();

        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != validApiKey)
        {
            return new UnauthorizedResult(); //Returns error
        }
        
        //Check if the provided ID is a positive number
        if (id <= 0)
            return new BadRequestObjectResult("The show ID must be a positive number");

        //Checks list of shows with same ID variable
        var showToDelete = await _context.Shows.FindAsync(id);

        //If no show exists, send back error
        if (showToDelete == null) 
            return new NotFoundResult();
        
        _context.Shows.Remove(showToDelete);
        await _context.SaveChangesAsync();
        return new OkObjectResult($"Deleted show with id: {id}");
    }

    [Function("UpdateShow")]
    public async Task<IActionResult> Update([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "shows/{id}")] HttpRequest req, int id)
    {
        //Fetch the key from Key Vault
        string validApiKey = await GetSecretKeyFromVault();

        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != validApiKey)
        {
            return new UnauthorizedResult(); //Returns error
        }
        //Checks list for existing show
        var existingShow = await _context.Shows.FindAsync(id);

        //If no show exists, send back error
        if (existingShow == null)
            return new NotFoundResult();

        //Read the new data from the request body
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        try
        {
            var updatedShowData = JsonSerializer.Deserialize<Show>(requestBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (updatedShowData == null)
                return new BadRequestObjectResult("Bad Request");

            //Updates properties of the existing show with the new data
            if (!string.IsNullOrEmpty(updatedShowData.Title))
                existingShow.Title = updatedShowData.Title;

            if (updatedShowData.ShowRunner != null)
                existingShow.ShowRunner = updatedShowData.ShowRunner;

            if (updatedShowData.Genre != null)
                existingShow.Genre = updatedShowData.Genre;

            if (updatedShowData.ReleaseYear != 0)
                existingShow.ReleaseYear = updatedShowData.ReleaseYear;

            if (updatedShowData.NumberOfSeasons != 0)
                existingShow.NumberOfSeasons = updatedShowData.NumberOfSeasons;
            
            if (updatedShowData.Distributor != null)
                existingShow.Distributor = updatedShowData.Distributor;

            await _context.SaveChangesAsync();
            return new OkObjectResult(existingShow);
        }
        catch (JsonException)
        {  
            return new BadRequestObjectResult("Invalid JSON format");
        }
    }

    [Function("ValidateShows")]
    public async Task<IActionResult> Validate([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "shows/validate")] HttpRequest req)
    {
        //Fetch the key from Key Vault
        string validApiKey = await GetSecretKeyFromVault();

        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != validApiKey)
        {
            _telemetry.TrackEvent("ValidationAuthFailed");
            return new UnauthorizedResult();
        }

        var allShows = await _context.Shows.ToListAsync();
        int updatedCount = 0;

        foreach (var show in allShows)
        {
            //If ReleaseYear is before 2005, set IsOld to true
            bool isOld = show.ReleaseYear < 2005;
            
            //Update if the status changed or it was never validated
            if (show.IsOld != isOld || show.LastValidated == null)
            {
                show.IsOld = isOld;
                show.LastValidated = DateTime.UtcNow;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        var eventProperties = new Dictionary<string, string> 
        { 
            { "UpdatedCount", updatedCount.ToString() },
            { "TriggeredBy", "LogicApp" }
        };
        
        _telemetry.TrackEvent("ValidationTriggered", eventProperties);

        return new OkObjectResult(new 
        { 
            UpdatedCount = updatedCount, 
            Timestamp = DateTime.UtcNow 
        });
    }

    private async Task<string> GetSecretKeyFromVault()
    {
        //Get the Vault URL
        string keyVaultUrl = _config["KeyVaultUrl"]!;

        //Create a client
        var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

        //Retrieve the secret named API Key
        KeyVaultSecret secret = await client.GetSecretAsync("ApiKey");
        
        return secret.Value;
    }
}

 //Database Context
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<shows_api.Show> Shows { get; set; }
}