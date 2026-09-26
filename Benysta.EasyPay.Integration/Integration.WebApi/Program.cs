using Integration.BusinessLogics;
using Integration.DatabaseAccess;
using Integration.WebApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Nibbs.Nps.Integration;
using Nibbs.Nps.Integration.Configuration;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddControllers();

// NIBSS National Payment Stack integration (configured via the "Nps" section).
builder.Services.Configure<NpsOptions>(builder.Configuration.GetSection(NpsOptions.SectionName));
builder.Services.AddNpsIntegration();

builder.Services.AddDALApplicationDependencies(builder.Configuration);

// Business-logic layer: MediatR commands/queries the controllers dispatch to.
builder.Services.AddIntegrationBusinessLogics();

// Request/response logging for the pacs.008 / pacs.002 credit transfer flow, both
// directions. Covers NPS certification Phases 1-3, including the inbound pacs.008 traffic
// Phase 2 expects you to be able to observe.
builder.Services.AddNpsSingleTransferLogging();

// Request/response logging for the acmt.023 / acmt.024 name enquiry flow, both directions.
builder.Services.AddNpsNameEnquiryLogging();

// Identification Verification flow: answer inbound acmt.023 name enquiries with acmt.024.
// PlaceholderAccountVerificationService rejects everything — swap in the core-banking lookup.
builder.Services.AddNpsIdentificationVerificationFlow<PlaceholderAccountVerificationService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply any pending EF Core migrations before the server starts accepting requests.
await app.MigrateNibssNpsDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Scalar API reference UI at /scalar/v1 for documentation and endpoint testing.
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("BENYSTA MFB NIBSS National Payment Stack (NPS) Integration API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

app.MapControllers();

//Todo[] sampleTodos =
//[
//    new(1, "Walk the dog"),
//    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
//    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
//    new(4, "Clean the bathroom"),
//    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
//];

//var todosApi = app.MapGroup("/todos");
//todosApi.MapGet("/", () => sampleTodos)
//        .WithName("GetTodos");

//todosApi.MapGet("/{id}", Results<Ok<Todo>, NotFound> (int id) =>
//    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
//        ? TypedResults.Ok(todo)
//        : TypedResults.NotFound())
//    .WithName("GetTodoById");

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
