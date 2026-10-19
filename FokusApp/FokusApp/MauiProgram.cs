using FokusApp.Components.ErrorComponent;
using FokusApp.Core.DataBase;
using FokusApp.Core.DataBase.ErrorLogging;
using FokusApp.Core.DataBase.Repositories.Implementations;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using FokusApp.Services;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Components.Layout;
using FokusApp.Components.Calender;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.PopUps.TaskEdit;
#if IOS || ANDROID
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Handlers;
#endif
using FokusApp.Core.Interfaces;


using FokusApp.Components.FocusTimerComponent;
using FokusApp.Components.Settings;
using FokusApp.Components.TaskOverViewComponent;
using System.Data;
using System.Globalization;



namespace FokusApp; 
public static class MauiProgram
{
    private static readonly TimeSpan MigrationLockStaleAfter = TimeSpan.FromMinutes(10);

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()

            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddLocalization();
        builder.Services.AddSingleton<IAppLocalizationService, AppLocalizationService>();
        builder.Services.AddSingleton<IAppStartupStateService, AppStartupStateService>();
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<ITimerSessionController, TimerSessionController>();
        builder.Services.AddSingleton<ITimerAndAudioService, TimerAndAudioService>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IDayChangeService, DayChangeService>();
        builder.Services.AddSingleton<IScopeService, ScopeService>();
        builder.Services.AddSingleton<ITaskCacheService, TaskCacheService>();
        builder.Services.AddSingleton<IActiveTaskService, ActiveTaskService>();
        builder.Services.AddSingleton<IRingtoneCatalogService, RingtoneCatalogService>();
        builder.Services.AddSingleton<IUserSettingsService, UserSettingsService>();
        builder.Services.AddSingleton<IAppDataCoordinator, AppDataCoordinator>();
        builder.Services.AddSingleton<IStartupDataChecker, StartupDataChecker>();
        builder.Services.AddSingleton<IUiBackPressService, UiBackPressService>();
        builder.Services.AddSingleton<ITaskPriorityFormatter, TaskPriorityFormatter>();
        builder.Services.AddSingleton<IAppEnvironmentInfo, MauiAppEnvironmentInfo>();
        builder.Services.AddSingleton<IAppErrorCaptureService, AppErrorCaptureService>();
        builder.Services.AddSingleton<IDataTransferFileService, MauiDataTransferFileService>();
        builder.Services.AddSingleton<IDataTransferTextProvider, LocalizedDataTransferTextProvider>();
        builder.Services.AddSingleton<IUiTextProvider, LocalizedUiTextProvider>();
        builder.Services.AddScoped<IDataImportExportService, DataImportExportService>();

#if ANDROID
        builder.Services.AddSingleton<ITimerPlatformAdapter, Platforms.Android.AndroidTimerPlatformAdapter>();
#else
        builder.Services.AddSingleton<ITimerPlatformAdapter, DefaultTimerPlatformAdapter>();
#endif
        
        builder.Services.AddScoped<IFocusTimerViewModel, FocusTimerViewModel>();
        builder.Services.AddScoped<ITaskOverViewViewModel, TaskOverViewViewModel>();
        builder.Services.AddScoped<ISettingsViewModel, SettingsViewModel>();
        builder.Services.AddScoped<ICalenderViewModel, CalenderViewModel>();
        builder.Services.AddScoped<IErrorPageViewModel, ErrorPageViewModel>();
        builder.Services.AddScoped<INavMenuViewModel, NavMenuViewModel>();
        builder.Services.AddTransient<IFilterComponentViewModel, FilterComponentViewModel>();
        builder.Services.AddTransient<ICalendarPopupViewModel, CalendarPopupViewModel>();
        builder.Services.AddTransient<ITaskEditPopUpViewModel, TaskEditPopUpViewModel>();
        builder.Services.AddTransient<Components.PopUps.TaskEdit.TaskEditorCoordinator>();

        builder.Services.AddSingleton<IAppErrorLogRepository, AppErrorLogRepository>();
        builder.Services.AddSingleton<IAppErrorLogService, AppErrorLogService>();
        builder.Services.AddSingleton<ILegacyAppErrorLogMigrationService, LegacyAppErrorLogMigrationService>();

        builder.Services.AddSingleton<ISettingRepository, SettingRepository>();
        builder.Services.AddSingleton<ITaskRepository, TaskRepository>();
        builder.Services.AddSingleton<IRingtonesRepository, RingtonesRepository>();
        builder.Services.AddSingleton<IDataImportRepository, DataImportRepository>();

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "FokusApp.db");
        var errorDatabasePath = Path.Combine(FileSystem.AppDataDirectory, "FokusApp.Errors.db");
        builder.Services.AddDbContextFactory<FokusAppDbContext>(
            options => options.UseSqlite(
                $"Data Source={databasePath}",
                sqlite => sqlite.MigrationsAssembly("FokusApp.Migrations")));
        builder.Services.AddDbContextFactory<AppErrorDbContext>(
            options => options.UseSqlite(
                $"Data Source={errorDatabasePath}",
                sqlite => sqlite.MigrationsAssembly("FokusApp.Migrations")));

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif



        ConfigureMauiHandlers(builder);

        var app = builder.Build();

        MigrateDatabases(app.Services);

        return app;
    }

    private static void MigrateDatabases(IServiceProvider services)
    {
        try
        {
            var errorDbFactory = services.GetRequiredService<IDbContextFactory<AppErrorDbContext>>();
            using var errorDb = errorDbFactory.CreateDbContext();
            RemoveAbandonedSqliteMigrationLock(errorDb);
            errorDb.Database.Migrate();

            var legacyErrorLogMigration = services.GetRequiredService<ILegacyAppErrorLogMigrationService>();
            legacyErrorLogMigration.CopyExistingLogsToErrorDatabase();

            var dbFactory = services.GetRequiredService<IDbContextFactory<FokusAppDbContext>>();
            using var db = dbFactory.CreateDbContext();
            RemoveAbandonedSqliteMigrationLock(db);
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            var errorCaptureService = services.GetRequiredService<IAppErrorCaptureService>();
            errorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = "MauiProgram.DatabaseMigration",
                    IsFatal = true
                }).GetAwaiter().GetResult();

            throw;
        }
    }

    private static void RemoveAbandonedSqliteMigrationLock(DbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                connection.Open();
            }

            using var tableExistsCommand = connection.CreateCommand();
            tableExistsCommand.CommandText =
                """
                SELECT COUNT(1)
                FROM sqlite_master
                WHERE type = 'table' AND name = '__EFMigrationsLock';
                """;
            if (Convert.ToInt32(
                    tableExistsCommand.ExecuteScalar(),
                    CultureInfo.InvariantCulture) == 0)
            {
                return;
            }

            using var findLockCommand = connection.CreateCommand();
            findLockCommand.CommandText =
                """
                SELECT "Timestamp"
                FROM "__EFMigrationsLock"
                WHERE "Id" = 1
                LIMIT 1;
                """;
            var rawTimestamp = findLockCommand.ExecuteScalar();

            var timestampText = Convert.ToString(rawTimestamp, CultureInfo.InvariantCulture);
            if (!DateTimeOffset.TryParse(
                    timestampText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var lockTimestamp) ||
                lockTimestamp > DateTimeOffset.UtcNow - MigrationLockStaleAfter)
            {
                return;
            }

            using var deleteLockCommand = connection.CreateCommand();
            deleteLockCommand.CommandText =
                """
                DELETE FROM "__EFMigrationsLock"
                WHERE "Id" = 1 AND "Timestamp" = $Timestamp;
                """;
            var timestampParameter = deleteLockCommand.CreateParameter();
            timestampParameter.ParameterName = "$Timestamp";
            timestampParameter.Value = timestampText;
            deleteLockCommand.Parameters.Add(timestampParameter);
            deleteLockCommand.ExecuteNonQuery();
        }
        finally
        {
            if (shouldCloseConnection)
            {
                connection.Close();
            }
        }
    }




    private static void ConfigureMauiHandlers(MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
#if IOS
                WebViewHandler.Mapper.AppendToMapping("DisableScroll", (handler, view) =>
                {
                    var wk = handler.PlatformView;
                    if (wk?.ScrollView != null)
                    {
                        wk.ScrollView.ScrollEnabled = false;
                        wk.ScrollView.Bounces = false;
                        wk.ScrollView.AlwaysBounceVertical = false;
                        wk.ScrollView.AlwaysBounceHorizontal = false;
                    }
                });

                BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("DisableScroll", (handler, view) =>
                {
                    var wk = handler.PlatformView;
                    if (wk?.ScrollView != null)
                    {
                        wk.ScrollView.ScrollEnabled = false;
                        wk.ScrollView.Bounces = false;
                        wk.ScrollView.AlwaysBounceVertical = false;
                        wk.ScrollView.AlwaysBounceHorizontal = false;
                    }
                });
#endif

#if ANDROID
            WebViewHandler.Mapper.AppendToMapping("DisableScroll", (handler, view) =>
            {
                var wv = handler.PlatformView;
                if (wv != null)
                {
                    wv.VerticalScrollBarEnabled = false;
                    wv.HorizontalScrollBarEnabled = false;
                    wv.OverScrollMode = Android.Views.OverScrollMode.Never;
                }
            });

            BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("DisableScroll", (handler, view) =>
            {
                var wv = handler.PlatformView;
                if (wv != null)
                {
                    wv.VerticalScrollBarEnabled = false;
                    wv.HorizontalScrollBarEnabled = false;
                    wv.OverScrollMode = Android.Views.OverScrollMode.Never;
                }
            });
#endif
        });
    }

}
