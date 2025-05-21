namespace ServizioChiamata;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    private Timer _timer;
    private string _executablePath;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _executablePath = _configuration["ExecutablePath"] ?? string.Empty;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PeriodicTimer periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(_configuration.GetValue<int>("IntervalloAttesa")));

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }




            await Task.Delay(1000, stoppingToken);
        }
    }
}
