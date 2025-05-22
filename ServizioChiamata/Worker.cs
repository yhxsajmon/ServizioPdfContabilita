using System.Diagnostics;

namespace ServizioChiamata;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _executablePath;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _executablePath = _configuration["ExecutablePath"] ?? string.Empty;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        //Timer di attesa
        var interval = TimeSpan.FromMinutes(_configuration.GetValue<int>("IntervalloAttesa"));
        using var periodicTimer = new PeriodicTimer(interval);
        //Loop di esecuzione
        //while (true)
        while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            try
            {
                if (!string.IsNullOrWhiteSpace(_executablePath))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = _executablePath,
                        WorkingDirectory = Path.GetDirectoryName(_executablePath) ?? "",
                        UseShellExecute = false // Important for services!
                    };
                    Process.Start(processInfo);
                    _logger.LogInformation("Process started: {executablePath}", _executablePath);
                }
                else
                {
                    _logger.LogWarning("Executable path is not set.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start process: {executablePath}", _executablePath);
            }           
        }
    }

    //StopAsync non necessario
}