using Microsoft.EntityFrameworkCore;
using ToDoAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
//builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();


// Seed data 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
    if (!db.Todos.Any())
    {
        db.Todos.AddRange(
            new Todo { Name = "Learn C#", IsComplete = true, Priority = Priority.High },
            new Todo { Name = "Build an API", IsComplete = false, Priority = Priority.Medium },
            new Todo { Name = "Write Documentation", IsComplete = false, Priority = Priority.Low }
        );
        db.SaveChanges();
    }
}

// MapGroup for /todoitems
var todoItems = app.MapGroup("/todoitems");

// GET all todo items
todoItems.MapGet("/", async (TodoDb db) =>
    await db.Todos.ToListAsync());

// GET completed todo items
todoItems.MapGet("/complete", async (TodoDb db) =>
    await db.Todos.Where(t => t.IsComplete).ToListAsync());

// GET todo item by id
todoItems.MapGet("/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());

// POST a new todo item
todoItems.MapPost("/", async (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

// PUT to update a todo item
todoItems.MapPut("/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;
    todo.Priority = inputTodo.Priority; // Update priority

    await db.SaveChangesAsync();

    return Results.NoContent();
});

// DELETE a todo item
todoItems.MapDelete("/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

// SEARCH todos by priority
todoItems.MapGet("/search/bypriority", async (string priority, TodoDb db) =>
{
    if (!Enum.TryParse<Priority>(priority, true, out var parsedPriority))
    {
        return Results.BadRequest("Invalid priority value.");
    }
    var todos = await db.Todos
        .Where(t => t.Priority == parsedPriority)
        .ToListAsync();
    return Results.Ok(todos);
});

app.Run();