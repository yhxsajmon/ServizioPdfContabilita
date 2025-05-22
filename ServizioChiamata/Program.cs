using ServizioChiamata;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("ConfigurazioneServizio.json", optional: false, reloadOnChange: true);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
