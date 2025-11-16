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

app.MapControllers();

app.Run();
