using System.Resources;
using System;
using DevToys.Api;
using DevToys.Blazor.BuiltInTools.ExtensionsManager;
using DevToys.Blazor.BuiltInTools;
using DevToys.Blazor.Core.Languages;
using DevToys.Blazor.Core.Services;
using DevToys.Blazor.Pages;
using DevToys.Core;
using DevToys.Core.Mef;
using DevToys.Core.Settings;
using DevToys.Web.Components;
using DevToys.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using DevToys.Business.ViewModels;

internal class Program
{
    private static MefComposer _mefComposer;

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ILoggerFactory loggerFactory = new LoggerFactory();
        LoggingExtensions.LoggerFactory = loggerFactory;


        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddLogging((builder) =>
        {
#if DEBUG
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif

            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
        });

        builder.Services.AddSingleton(provider => _mefComposer.Provider);
        builder.Services.AddSingleton<IWindowService, WindowService>();
        builder.Services.AddScoped<DocumentEventService, DocumentEventService>();
        builder.Services.AddScoped<PopoverService, PopoverService>();
        builder.Services.AddScoped<ContextMenuService, ContextMenuService>();
        builder.Services.AddScoped<GlobalDialogService, GlobalDialogService>();
        builder.Services.AddScoped<UIDialogService, UIDialogService>();
        builder.Services.AddScoped<FontService, FontService>();

        InitServices();
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(MainLayout).Assembly);
        app.Run();
    }
    private static void InitServices()
    {
        // Clear older temp files.
        FileHelper.ClearTempFiles(Constants.AppTempFolder);

        // Initialize extension installation folder, and uninstall extensions that are planned for being removed.
        string[] pluginFolders
            = new[]
            {
                Path.Combine(AppContext.BaseDirectory!, "Plugins"),
                Constants.PluginInstallationFolder
            };
        ExtensionInstallationManager.PreferredExtensionInstallationFolder = Constants.PluginInstallationFolder;
        ExtensionInstallationManager.ExtensionInstallationFolders = pluginFolders;
        ExtensionInstallationManager.UninstallExtensionsScheduledForRemoval();

        // Initialize MEF.
        _mefComposer
            = new MefComposer(
                assemblies: new[] {
                    typeof(MainWindowViewModel).Assembly,
                    typeof(TitleBarInfoProvider).Assembly,
                    typeof(ThemeListener).Assembly,
                    typeof(DevToysBlazorResourceManagerAssemblyIdentifier).Assembly
                },
                pluginFolders);


        // Set the user-defined language.
        LanguageDefinition languageDefinition
            = LanguageManager.Instance.AvailableLanguages.FirstOrDefault()
            ?? LanguageManager.Instance.AvailableLanguages[0];
        LanguageManager.Instance.SetCurrentCulture(languageDefinition);

        // Load the UI.
        //_mefComposer.Provider.Import<IThemeListener>();
        //_mefComposer.Provider.Import<TitleBarInfoProvider>();
    }
}
internal static class Constants
{
    internal static readonly string AppCacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "AppData");

    internal static string PluginInstallationFolder => Path.Combine(AppCacheDirectory, "Plugins");

    internal static string AppTempFolder => Path.Combine(AppCacheDirectory, "Temp");
}
