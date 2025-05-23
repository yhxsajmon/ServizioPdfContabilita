using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace ServizioChiamata;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _executablePath;
    private readonly TimeSpan _interval;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _executablePath = _configuration["ExecutablePath"] 
                          ?? throw new ArgumentNullException("ExecutablePath non configurato");
        _interval = TimeSpan.FromMinutes(_configuration.GetValue<int>("IntervalloAttesa"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker partito: eseguirò {Exe} ogni {Min} minuti", 
                                _executablePath, _interval.TotalMinutes);

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunProcessAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Si è richiesto lo stop
            _logger.LogInformation("Worker sta terminando per cancellation.");
        }
    }

    private async Task RunProcessAsync(CancellationToken ct)
    {
        _logger.LogInformation("Avvio processo esterno: {Exe}", _executablePath);

        var psi = new ProcessStartInfo
        {
            FileName = _executablePath,
            WorkingDirectory = Path.GetDirectoryName(_executablePath) ?? "",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        proc.OutputDataReceived += (s, e) => { if (e.Data != null) _logger.LogInformation(e.Data); };
        proc.ErrorDataReceived  += (s, e) => { if (e.Data != null) _logger.LogError(e.Data);   };

        try
        {
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            // Attendiamo in modo asincrono e rispettando la cancellation
            await proc.WaitForExitAsync(ct);

            _logger.LogInformation("Process exited with code {Code}", proc.ExitCode);

            if (proc.ExitCode != 0)
                _logger.LogWarning("Processo terminato con codice di errore.");
        }
        catch (OperationCanceledException)
        {
            // Se ci chiedono la cancel, proviamo a chiuderlo
            if (!proc.HasExited)
            {
                _logger.LogWarning("Cancellazione in corso, chiedo al processo di terminare...");
                proc.Kill(true);
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore eseguendo il processo");
        }
    }

    // Facoltativo: qui puoi fare cleanup aggiuntivo
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync chiamato su Worker");
        return base.StopAsync(cancellationToken);
    }
}
