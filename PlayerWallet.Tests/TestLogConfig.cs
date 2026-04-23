using NLog;
using NLog.Config;
using NLog.Targets;

namespace PlayerWallet.Tests;

public static class TestLogConfig
{
    public static LoggingConfiguration Create()
    {
        var config = new LoggingConfiguration();

        var fileTarget = new FileTarget("testFile")
        {
            FileName = "logs/tests-${shortdate}.log",
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}",
            KeepFileOpen = true
        };

        var consoleTarget = new ConsoleTarget("console")
        {
            Layout = "${longdate}|${level:uppercase=true}|${logger:shortName=true}|${message}"
        };

        config.AddTarget(fileTarget);
        config.AddTarget(consoleTarget);

        config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);

        return config;
    }
}