using System;
using System.IO;

namespace UnykachAio240Display;

public static class Logger {
    private static readonly string logFilePath = Path.Combine(AppConstants.SettingsDirectory, "log.txt");

    public static void WriteLine(string message, int? updateFrequencySeconds, string? sensorIdentifier) {
        string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd}] - {updateFrequencySeconds}, {sensorIdentifier ?? "null"} - {message}";
        File.AppendAllText(logFilePath, formattedMessage + Environment.NewLine);
    }
}