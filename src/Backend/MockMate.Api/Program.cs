using MockMate.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIdentityAuth(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapFeatureEndpoints();

app.MapGet("/", () => Results.Ok("Welcome to MockMate API!"));
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
