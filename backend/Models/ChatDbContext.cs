using System;
using Microsoft.EntityFrameworkCore;

namespace ChatApi.Models;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    // Esta propriedade se tornará a tabela "Messages" no banco de dados
    public DbSet<Message> Messages { get; set; }

}
