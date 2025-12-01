using AI_Server;
using AI_Server.Infrastructure.Models.IntentModel;
using Microsoft.ML;
using Microsoft.Extensions.ML;
using AI_Server.Repositories.GenericClassificationModelRepo.Interfaces;
using AI_Server.Repositories.GenericClassificationModelRepo.Services;
using AI_Server.Infrastructure.Models.MoodModel;
using AI_Server.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#region register_modelEngine
builder.Services.AddPredictionEnginePool<MoodInput, MoodOutput>()
    .FromFile("./Models/Mood/MoodModel/MoodModel.mlnet");
builder.Services.AddPredictionEnginePool<IntentInput, IntentOutput>()
    .FromFile("./Models/Intent/IntentModel/IntentModel.mlnet");
#endregion
builder.Services
    .AddScoped
    <IGenericClassificationModel<MoodOutput,MoodInput>
    ,GenericClassificationModelService<MoodOutput,MoodInput>>();
builder.Services
    .AddScoped
    <IGenericClassificationModel<IntentOutput, IntentInput>
    , GenericClassificationModelService<IntentOutput, IntentInput>>();

builder.Services.AddAutoMapper(op => op.AddProfile(typeof(MappingProfile)));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage(); // Shows detailed error page
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
