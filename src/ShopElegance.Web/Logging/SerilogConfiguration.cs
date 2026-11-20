using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace ShopElegance.Web.Logging;

public static class SerilogConfiguration
{
    public static LoggerConfiguration Configure(
        LoggerConfiguration loggerConfig,
        IConfiguration configuration,
        string environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        var columnOptions = new ColumnOptions
        {
            AdditionalColumns = new Collection<SqlColumn>
            {
                new SqlColumn { ColumnName = "UserId",      DataType = SqlDbType.NVarChar, DataLength = 450,  AllowNull = true },
                new SqlColumn { ColumnName = "RequestPath", DataType = SqlDbType.NVarChar, DataLength = 1000, AllowNull = true },
                new SqlColumn { ColumnName = "RequestId",   DataType = SqlDbType.NVarChar, DataLength = 100,  AllowNull = true },
                new SqlColumn { ColumnName = "IPAddress",   DataType = SqlDbType.NVarChar, DataLength = 64,   AllowNull = true },
                new SqlColumn { ColumnName = "Environment", DataType = SqlDbType.NVarChar, DataLength = 50,   AllowNull = true }
            }
        };
        columnOptions.Store.Remove(StandardColumn.Properties); // stored in AdditionalColumns
        columnOptions.Store.Remove(StandardColumn.MessageTemplate);

        return loggerConfig
            .MinimumLevel.Is(environment == "Production" ? LogEventLevel.Warning : LogEventLevel.Debug)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command",
                environment == "Development" ? LogEventLevel.Information : LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "Practora")
            // Console (development only)
            .WriteTo.Conditional(
                _ => environment == "Development",
                wt => wt.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"))
            // Rolling file — all logs
            .WriteTo.File(
                path: "Logs/practora-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 50_000_000,
                rollOnFileSizeLimit: true)
            // Rolling file — errors only
            .WriteTo.File(
                path: "Logs/errors-.log",
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                retainedFileCountLimit: 60,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            // SQL Server sink
            .WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions
                {
                    TableName        = "ApplicationLogs",
                    AutoCreateSqlTable = true,
                    BatchPostingLimit = 50,
                    BatchPeriod      = TimeSpan.FromSeconds(5)
                },
                columnOptions: columnOptions,
                restrictedToMinimumLevel: LogEventLevel.Information);
    }
}
