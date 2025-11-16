using VibeNet.Config;
using VibeNet.Helper;
using VibeNet.Interfaces;
using VibeNet.Services;
using VibeNet.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<AzureSettings>(options =>
{
    // SQL + Cosmos + BlobStorage from ConnectionStrings
    options.SqlDatabase = builder.Configuration["ConnectionStrings:SqlDatabase"];
    options.CosmosDb = builder.Configuration["ConnectionStrings:CosmosDb"];
    options.BlobConnectionString = builder.Configuration["ConnectionStrings:BlobStorage"];

    // BaseUrl + ImagesContainer from AzureStorage section
    options.BaseUrl = builder.Configuration["AzureStorage:BaseUrl"];
    options.ImagesContainer = builder.Configuration["AzureStorage:ImagesContainer"];
});
builder.Services.Configure<ProfilePictureSettings>(builder.Configuration.GetSection("ProfilePicture"));

builder.Services.AddSingleton<Helpers>();
builder.Services.AddSingleton<CosmosService>();
builder.Services.AddSingleton<BlobService>();
builder.Services.AddScoped<IUsers, UsersService>();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<DeletedUserMiddleware>();

app.UseAuthorization();

app.MapGet("/env-check", (IConfiguration config) =>
{
    return Results.Json(new
    {
        sql = config["ConnectionStrings:SqlDatabase"],
        cosmos = config["ConnectionStrings:CosmosDb"],
        blob = config["ConnectionStrings:BlobStorage"],
        baseUrl = config["AzureStorage:BaseUrl"],
        images = config["AzureStorage:ImagesContainer"],
        jwt = config["Jwt:SecretKey"]
    });
});

app.MapGet("/test-sql", async (IConfiguration config) =>
{
    try
    {
        var cs = config["ConnectionStrings:SqlDatabase"];
        if (string.IsNullOrEmpty(cs))
            return Results.Problem("SQL connection string is empty.", statusCode: 500);

        using var conn = new Microsoft.Data.SqlClient.SqlConnection(cs);
        await conn.OpenAsync();                      // will throw if blocked/invalid
        using var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT 1", conn);
        var val = await cmd.ExecuteScalarAsync();
        await conn.CloseAsync();

        return Results.Ok(new { ok = true, ping = val });
    }
    catch (Exception ex)
    {
        // Return exception message (safe for debugging). Remove this in production.
        return Results.Problem(detail: ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : ""), statusCode: 500);
    }
});
app.MapGet("/test-cosmos", async (IConfiguration config) =>
{
    try
    {
        var conn = config["ConnectionStrings:CosmosDb"];
        if (string.IsNullOrEmpty(conn))
            return Results.Problem("Cosmos connection string missing", statusCode: 500);

        var client = new Microsoft.Azure.Cosmos.CosmosClient(conn);

        // Test DB
        var db = client.GetDatabase("vibenetdb");
        var dbResponse = await db.ReadAsync();

        // Test container
        var container = client.GetContainer("vibenetdb", "connections");
        await container.ReadContainerAsync();

        return Results.Ok(new { ok = true, db = dbResponse.Resource.Id });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});
app.MapGet("/test-blob", async (IConfiguration config) =>
{
    try
    {
        var conn = config["ConnectionStrings:BlobStorage"];
        var containerName = config["AzureStorage:ImagesContainer"];

        var c = new Azure.Storage.Blobs.BlobContainerClient(conn, containerName);
        var info = await c.ExistsAsync();

        return Results.Ok(new { ok = true, exists = info.Value });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

app.MapControllers();

app.Run();
