using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Midterm.Project;

public class shows_api
{
    private readonly ILogger<shows_api> _logger;
    private readonly IConfiguration _config;
    private static readonly List<Show> shows = new();
    public shows_api(ILogger<shows_api> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
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

    }

    [Function("GetShows")] //Returns shows
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != _config["ApiKey"])
        {
            return new UnauthorizedResult(); //Returns error
        }
        return new OkObjectResult(shows);
    }

    [Function("GetShowById")]
    public IActionResult GetById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shows/{id}")] 
    HttpRequest req, int id)
    {   
        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != _config["ApiKey"])
            return new UnauthorizedResult(); //Returns error

        //Check if the provided ID is a positive number
        if (id <= 0)
            return new BadRequestObjectResult("The show ID must be a positive number");
        var show = shows.FirstOrDefault(s => s.Id == id);

        //If no show is found with that ID, return a 404 Not Found error
        if (show == null)
            return new NotFoundResult();

        //If the show is found, return it
        return new OkObjectResult(show);
    }

    [Function("CreateShow")] //Creates a show
    public async Task<IActionResult> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != _config["ApiKey"])
        {
            return new UnauthorizedResult(); //Returns error
        }
        //Takes JSON data and makes into a variable
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var newShow = JsonSerializer.Deserialize<Show>(body);

        //Checks if newShow variable is valid 
        if (newShow == null)
            return new BadRequestObjectResult("Bad Request");

        if (string.IsNullOrWhiteSpace(newShow.Title))
            return new BadRequestObjectResult("The 'Title' field is required");

        int maxId = shows.Any() ? shows.Max(s => s.Id) : 0;
        newShow.Id = maxId + 1;
        shows.Add(newShow); //Adds show to list
        return new OkObjectResult(newShow);
    }


    [Function("DeleteShow")] //Deletes a show
    public IActionResult Delete([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "shows/{id}")] HttpRequest req, int id)
    {
        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != _config["ApiKey"])
        {
            return new UnauthorizedResult(); //Returns error
        }
        //Checks list of shows with same ID variable
        var showToDelete = shows.FirstOrDefault(s => s.Id == id);
       
        //Check if the provided ID is a positive number
        if (id <= 0)
            return new BadRequestObjectResult("The show ID must be a positive number");

        //If no show exists, send back error
        if (showToDelete == null)
            return new NotFoundResult();
        shows.Remove(showToDelete); //Removes the show from list
        return new OkObjectResult($"Deleted show with id: {id}");
    }

    [Function("UpdateShow")]
    public async Task<IActionResult> Update([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "shows/{id}")] HttpRequest req, int id)
    {
        //API Authentication
        if (!req.Headers.TryGetValue("x-api-key", out var sentApiKey) || sentApiKey.FirstOrDefault() != _config["ApiKey"])
        {
            return new UnauthorizedResult(); //Returns error
        }
        //Checks list for existing show
        var existingShow = shows.FirstOrDefault(s => s.Id == id);

        //If no show exists, send back error
        if (existingShow == null)
            return new NotFoundResult();

        //Read the new data from the request body
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updatedShowData = JsonSerializer.Deserialize<Show>(requestBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (updatedShowData == null || string.IsNullOrWhiteSpace(updatedShowData.Title))
            return new BadRequestObjectResult("The 'Title' field is required");

        //Checks if existing data has changed
        bool noChangesDetected = existingShow.Title == updatedShowData.Title &&
                                 existingShow.ShowRunner == updatedShowData.ShowRunner &&
                                 existingShow.Genre == updatedShowData.Genre &&
                                 existingShow.ReleaseYear == updatedShowData.ReleaseYear &&
                                 existingShow.NumberOfSeasons == updatedShowData.NumberOfSeasons &&
                                 existingShow.Distributor == updatedShowData.Distributor;

        if (noChangesDetected)
            return new BadRequestObjectResult("No changes were detected");

        //Updates properties of the existing show with the new data
        existingShow.Title = updatedShowData.Title;
        existingShow.ShowRunner = updatedShowData.ShowRunner;
        existingShow.Genre = updatedShowData.Genre;
        existingShow.ReleaseYear = updatedShowData.ReleaseYear;
        existingShow.NumberOfSeasons = updatedShowData.NumberOfSeasons;
        existingShow.Distributor = updatedShowData.Distributor;

        return new OkObjectResult(existingShow);
    }
}