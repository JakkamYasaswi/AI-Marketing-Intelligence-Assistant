
using MarketingIntelligence.API.Models;
using MarketingIntelligence.API.Services;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// CORS configuration for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Gemini configuration
builder.Services.Configure<GeminiSettings>(
    builder.Configuration.GetSection("Gemini"));

// Register application services
builder.Services.AddScoped<GeminiService>();

// Register IP address validator
builder.Services.AddSingleton<IpAddressValidator>();

// Configure HttpClient with validated IP connections
builder.Services.AddHttpClient<WebsiteCrawlerService>()
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        var ipValidator =
            serviceProvider.GetRequiredService<IpAddressValidator>();

        return new SocketsHttpHandler
        {
            // Disable automatic redirects
            AllowAutoRedirect = false,

            // Connect only to an IP address that passed validation
            ConnectCallback = async (context, cancellationToken) =>
            {
                var requestUri =
                    context.InitialRequestMessage.RequestUri;

                if (requestUri == null)
                {
                    throw new HttpRequestException(
                        "Request URL is missing.");
                }

                var addresses =
                    await ipValidator.GetValidatedPublicAddressesAsync(
                        requestUri.ToString());

                foreach (var address in addresses)
                {
                    var socket = new Socket(
                        address.AddressFamily,
                        SocketType.Stream,
                        ProtocolType.Tcp);

                    try
                    {
                        await socket.ConnectAsync(
                            new IPEndPoint(
                                address,
                                context.DnsEndPoint.Port),
                            cancellationToken);

                        return new NetworkStream(
                            socket,
                            ownsSocket: true);
                    }
                    catch (SocketException)
                    {
                        socket.Dispose();
                    }
                }

                throw new HttpRequestException(
                    "Unable to connect to a validated public IP address.");
            }
        };
    });

// Swagger configuration
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS for React frontend
app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
