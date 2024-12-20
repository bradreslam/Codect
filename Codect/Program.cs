using BLL.Database;
using Microsoft.EntityFrameworkCore;
using DAL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowReactApp", builder =>
	{
		builder.WithOrigins("http://localhost:5173")  // React app URL
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options =>
{
	options.ListenAnyIP(8080);
	options.ListenAnyIP(8081);
});

builder.Services.AddDbContext<CodectEfCoreDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("CodectEfCoreDbContext"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<CodectEfCoreDbContext>();
	var retryCount = 0;
	const int maxRetries = 5;

	while (retryCount < maxRetries)
	{
		try
		{
			dbContext.Database.Migrate();
			if (dbContext.Database.CanConnect())
			{
				Console.WriteLine("Successfully connected to the database.");
				break;
			}
		}
		catch (Exception ex)
		{
			retryCount++;
			Console.WriteLine($"Attempt {retryCount} failed to connect to the database. Error: {ex.Message}");
			if (retryCount >= maxRetries)
			{
				throw new Exception("Failed to connect to the database after multiple attempts.", ex);
			}
			Task.Delay(5000).Wait(); // Wait for 5 seconds before retrying
		}
	}
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
	app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowReactApp");


app.UseAuthorization();

app.MapControllers();

app.Run();
