using Microsoft.EntityFrameworkCore;

using TodoApi;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = Environment.GetEnvironmentVariable("ToDoDB");
using (var connection = new MySqlConnection(connectionString))
{
    connection.Open();
    string version = connection.ServerVersion;
    Console.WriteLine($"MySQL Server Version: {version}");
}

Console.WriteLine($"🔍 Connection String: {connectionString}");

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 41)),
    mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

builder.Services.AddCors(o => o.AddPolicy("AllowAll", opt =>
    opt.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

var app = builder.Build();
app.UseCors("AllowAll");
// if (app.Environment.IsDevelopment()){
app.UseSwagger();
app.UseSwaggerUI();
// }
app.MapGet("/", async (ToDoDbContext Db) =>
{
    return await Db.Items.ToListAsync();
});

app.MapPost("/", async (Item item, ToDoDbContext Db) =>
{
    var todoItem = new Item
    {
        IsComplete = item.IsComplete,
        Name = item.Name
    };
    Console.WriteLine("💙💙",todoItem);

    Db.Items.Add(todoItem);
    await Db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todoItem.Id}", todoItem);
});

app.MapPut("/{id}", async (int Id, bool IsComplete, ToDoDbContext Db) =>
{
    var todo = await Db.Items.FindAsync(Id);
    if (todo is null) return Results.NotFound();
    todo.IsComplete = IsComplete;

    await Db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/{id}", async (int Id, ToDoDbContext Db) =>
{
    if (await Db.Items.FindAsync(Id) is Item todo)
    {
        Db.Items.Remove(todo);
        await Db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
});
app.Run();