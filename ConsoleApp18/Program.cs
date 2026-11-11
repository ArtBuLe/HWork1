using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    // Лимит времени выполнения процесса (в секундах)
    private const int TimeoutSeconds = 10;

    public static async Task Main()
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory
        };

        startInfo.ArgumentList.Add("--info");

        using Process process = new()
        {
            StartInfo = startInfo
        };

        Stopwatch stopwatch = new();
        DateTime startTime = DateTime.Now;

        int? pid = null;
        int? exitCode = null;
        TimeSpan duration = TimeSpan.Zero;
        string errorMessage = null;

        try
        {
            stopwatch.Start();
            process.Start();
            pid = process.Id;
            Console.WriteLine($"PID: {pid}");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

            try
            {
                await process.WaitForExitAsync(cts.Token);
                exitCode = process.ExitCode;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[!] Превышено время выполнения ({TimeoutSeconds} сек.). Принудительное завершение процесса...");
                errorMessage = $"Timeout: процесс превысил лимит в {TimeoutSeconds} секунд.";

                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                }
                catch (InvalidOperationException)
                {
                    process.Kill(true);
                }

                await process.WaitForExitAsync();
                exitCode = process.ExitCode;
            }
            stopwatch.Stop();
            duration = stopwatch.Elapsed;
            Console.WriteLine($"Длительность: {duration}");
            Console.WriteLine($"Exit code: {exitCode}");
        }
        catch (Win32Exception ex)
        {
            errorMessage = ex.Message;
            Console.WriteLine($"Ошибка запуска: Не удалось выполнить '{startInfo.FileName}'.");
            Console.WriteLine($"Детали ошибки: {errorMessage}");

            stopwatch.Stop();
            duration = stopwatch.Elapsed;
        }
        WriteToLog(startInfo.FileName, startInfo.ArgumentList, startTime, pid, duration, exitCode, errorMessage);
    }

    private static void WriteToLog
        (
        string fileName,
        IList<string> arguments,
        DateTime startTime,
        int? pid,
        TimeSpan duration,
        int? exitCode,
        string errorMessage
        )
    {
        string logEntry = $"[{startTime:yyyy-MM-dd HH:mm:ss}]\n" +
                          $"Program: {fileName}\n" +
                          $"Arguments: {string.Join(" ", arguments)}\n";

        if (pid.HasValue)
        {
            logEntry += $"PID: {pid.Value}\n";
        }
        logEntry += $"Duration: {duration:hh\\:mm\\:ss\\.fff}\n";

        if (exitCode.HasValue)
        {
            logEntry += $"ExitCode: {exitCode.Value}\n";
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logEntry += $"Error: {errorMessage}\n";
        }

        logEntry += "--------------------------------\n";

        // Добавление записи в конец файла (создает файл, если он не существует)
        File.AppendAllText("process-log.txt", logEntry);
    }
}