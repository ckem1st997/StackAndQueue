using StackAndQueue.QueueService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(typeof(IBackgroundTaskQueue<>), typeof(DefaultBackgroundTaskQueue<>));
builder.Services.AddSingleton(typeof(IBackgroundTaskStack<>), typeof(DefaultBackgroundTaskStack<>));
builder.Services.AddHostedService<QueueHostedService>();
builder.Services.AddHostedService<StackHostedService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
