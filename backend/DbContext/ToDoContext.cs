using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Context;

public class ToDoContext : DbContext
{
    public ToDoContext(DbContextOptions<ToDoContext> options) : base(options)
    {
    }

    public DbSet<ToDo> ToDos { get; set; }
}
