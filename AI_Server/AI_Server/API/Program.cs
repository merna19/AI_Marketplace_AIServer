using AI_Server;
using AI_Server.Infrastructure.Interfaces.IntentModel;
using Microsoft.ML;
using Microsoft.Extensions.ML;
using AI_Server.Infrastructure.Interfaces.MoodModel;
using AI_Server.Application.Mapping;
using AI_Server.Infrastructure.Repositories.GenericClassificationModelRepo.Interfaces;
using AI_Server.Infrastructure.Repositories.GenericClassificationModelRepo.Services;
// Add your Embeddings namespace
using Embeddings.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. REGISTER SERVICES
// ---------------------------------------------------------

// --- Existing AI_Server Services ---
#region register_modelEngine
builder.Services.AddPredictionEnginePool<MoodInput, MoodOutput>()
    .FromFile("./Infrastructure/Models/Mood/MoodModel/MoodModel.mlnet");
builder.Services.AddPredictionEnginePool<IntentInput, IntentOutput>()
    .FromFile("./Infrastructure/Models/Intent/IntentModel/IntentModel.mlnet");
#endregion

builder.Services.AddScoped<IGenericClassificationModel<MoodOutput, MoodInput>, GenericClassificationModelService<MoodOutput, MoodInput>>();
builder.Services.AddScoped<IGenericClassificationModel<IntentOutput, IntentInput>, GenericClassificationModelService<IntentOutput, IntentInput>>();

builder.Services.AddAutoMapper(op => op.AddProfile(typeof(MappingProfile)));
builder.Services.AddControllers();

// --- New Vector Search Services ---
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<ChromaService>();
builder.Services.AddScoped<ProductService>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
    }
    return new ProductService(connectionString);
});

// --- Infrastructure ---
builder.Services.AddOpenApi(); // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", cors =>
    {
        cors.WithOrigins("http://localhost:4200")  // Angular dev server origin
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------------------------------------------------------
// 2. INITIALIZATION
// ---------------------------------------------------------

// Initialize ChromaDB
try
{
    // We create a scope just to get the singleton service cleanly if needed, 
    // or just resolve directly since it's a Singleton.
    var chromaService = app.Services.GetRequiredService<ChromaService>();
    await chromaService.InitializeAsync();
    app.Logger.LogInformation("ChromaDB initialized successfully");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to initialize ChromaDB.");
    app.Logger.LogWarning("Application will start but ChromaDB operations may fail");
}

// ---------------------------------------------------------
// 3. HTTP PIPELINE
// ---------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();
app.UseCors("DevCors");
app.UseAuthorization();

// Map existing Controllers
app.MapControllers();

// ---------------------------------------------------------
// 4. MINIMAL APIs (Vector Search)
// ---------------------------------------------------------

// Health check endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        service = "AI Server & Vector Search API"
    });
})
.WithName("HealthCheck")
.WithTags("Database Health Check");

// Index all products
app.MapPost("/api/products/index", async (ProductService productService, ChromaService chroma, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Starting product indexing...");
        var products = await productService.GetAllProductsAsync();

        if (products.Count == 0)
        {
            return Results.Ok(new { message = "No products to index" });
        }

        await chroma.IndexProductsAsync(products);
        return Results.Ok(new { message = $"Indexed {products.Count} products", count = products.Count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error indexing products");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("IndexProducts")
.WithTags("Product Indexing");

// Index single product
app.MapPost("/api/products/{id}/index", async (int id, ProductService productService, ChromaService chroma, ILogger<Program> logger) =>
{
    try
    {
        var product = await productService.GetProductByIdAsync(id);
        if (product == null) return Results.NotFound(new { message = $"Product {id} not found" });

        await chroma.IndexProductAsync(product.Id, product.Name, product.Description, product.Category);
        return Results.Ok(new { message = "Product indexed successfully", productId = id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error indexing product {Id}", id);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("IndexProduct")
.WithTags("Product Indexing");

// Semantic search endpoint (With Min Score Filtering)
app.MapGet("/api/products/search", async (string query, ChromaService chroma, ILogger<Program> logger, int top = 5, double minScore = 0.3) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest(new { message = "Query parameter is required" });
        }

        logger.LogInformation("Searching for: {Query} (min score: {MinScore})", query, minScore);

        // 1. Get raw results from Chroma
        var results = await chroma.SearchAsync(query, top);

        // 2. Filter results by minimum score here
        // (Ideally pass minScore to SearchAsync for efficiency, but this works fine for small datasets)
        var filteredResults = results.Where(r => r.Score >= minScore).ToList();

        logger.LogInformation("Found {TotalCount} raw results, {FilteredCount} valid matches",
            results.Count, filteredResults.Count);

        return Results.Ok(new
        {
            query,
            minScore,
            totalResults = results.Count,
            validResults = filteredResults.Count,
            results = filteredResults
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error searching products");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("SearchProducts")
.WithTags("Product Search");

// Clear embeddings
app.MapDelete("/api/products/embeddings", async (ChromaService chroma, ILogger<Program> logger) =>
{
    try
    {
        await chroma.ClearAllAsync();
        return Results.Ok(new { message = "All embeddings cleared" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error clearing embeddings");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("ClearEmbeddings")
.WithTags("Clear Product Embeddings");

app.Run();