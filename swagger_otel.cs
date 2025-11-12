using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// 🔹 OpenTelemetry Tracing 설정
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("api-server"))
            .AddAspNetCoreInstrumentation()    // HTTP 요청 (Swagger 포함)
            .AddHttpClientInstrumentation()    // API 간 호출
            .AddSource("Dapr")                 // Dapr trace 연결
            .AddZipkinExporter(options =>
            {
                options.Endpoint = new Uri("http://zipkin.default.svc.cluster.local:9411/api/v2/spans");
            });
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
