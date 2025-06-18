using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using ChatApi.Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// --- 1. Configuração do CORS --- 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- 2. Configuração da Autenticação JWT --- 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
        };
    });

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Define o Título e a Versão da sua API
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ChatApi MVP", Version = "v1" });

    // Define o esquema de segurança (JWT Bearer Token)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Para se autenticar, use o endpoint /api/auth/login para obter um token. " +
                      "Depois, insira 'Bearer ' (com espaço) seguido pelo seu token neste campo. \n\n" +
                      "Exemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    // Adiciona o requisito de segurança que aplica o esquema definido acima a todos os endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- 3. Configuração do DbContext do EF Core com SQLite ---
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- Middlewares --- 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

// --- 4. Configuração dos Endpoints da API ---

// Endpoint de Login (não protegido) 
app.MapPost("/api/auth/login", (UserLogin user) =>
{
    if ((user.Username.Equals("aecio", StringComparison.CurrentCultureIgnoreCase) || user.Username.Equals("roberta", StringComparison.CurrentCultureIgnoreCase)) && user.Password == "123")
    {
        var claims = new[] { new Claim(ClaimTypes.Name, user.Username) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );
        return Results.Ok(new { token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token) });
    }
    return Results.Unauthorized();
});

// Endpoint para enviar mensagem (protegido por JWT)
app.MapPost("/api/messages", async (Message message, HttpContext http, ChatDbContext db) =>
{
    var senderUsername = http.User.FindFirst(ClaimTypes.Name)?.Value;
    if (string.IsNullOrEmpty(senderUsername))
    {
        return Results.Unauthorized();
    }

    message.Sender = senderUsername;
    message.TimeStamp = DateTime.UtcNow;

    db.Messages.Add(message);
    await db.SaveChangesAsync();

    return Results.Created($"/api/messages/{message.Id}", message);
}).RequireAuthorization();

// Endpoint para buscar mensagens (protegido por JWT)
app.MapGet("/api/messages/{otherUser}", async (string otherUser, HttpContext http, ChatDbContext db) =>
{
    var currentUser = http.User.FindFirst(ClaimTypes.Name)?.Value;
    if (string.IsNullOrEmpty(currentUser))
    {
        return Results.Unauthorized();
    }

    var messages = await db.Messages
        .Where(m => (m.Sender == currentUser && m.Receiver == otherUser) ||
                    (m.Sender == otherUser && m.Receiver == currentUser))
        .OrderBy(m => m.TimeStamp)
        .ToListAsync();

    return Results.Ok(messages);
}).RequireAuthorization();

// --- Inicialização do Banco de Dados SQLite (se não existir) ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    // Garante que o banco de dados e as tabelas sejam criados na primeira execução.
    dbContext.Database.EnsureCreated();
}

app.Run();