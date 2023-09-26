﻿using DevToys.Blazor.Components.Monaco.Editor;
using DevToys.Core.Settings;

namespace DevToys.Blazor.Components;

public partial class MonacoEditorDiff : RicherMonacoEditorDiffBase
{
    private readonly object _lock = new();
    private bool _isLoaded;
    private bool _oldIsActuallyEnabled;
    private bool _oldReadOnlyState;

#pragma warning disable IDE0044 // Add readonly modifier
    [Import]
    private IThemeListener _themeListener = default!;

    [Import]
    private ISettingsProvider _settingsProvider = default!;
#pragma warning restore IDE0044 // Add readonly modifier

    [Parameter]
    public string? Header { get; set; }

    [Parameter]
    public Func<MonacoEditorDiff, StandaloneDiffEditorConstructionOptions>? ConstructionOptions { get; set; }

    private bool ShowLoading { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _themeListener.ThemeChanged += ThemeListener_ThemeChanged;
        _settingsProvider.SettingChanged += SettingsProvider_SettingChanged;
        _oldIsActuallyEnabled = IsActuallyEnabled;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Guard.IsNull(OriginalEditor);
            Guard.IsNull(ModifiedEditor);

            InvokeAsync(DisplayLoadingIfSlowLoadingAsync).Forget();

            // Get desired options
            StandaloneDiffEditorConstructionOptions options = ConstructionOptions?.Invoke(this) ?? new();

            // Overwrite some important options.
            options.Theme = GetTheme();
            options.AutomaticLayout = true;
            options.GlyphMargin = false;
            options.MouseWheelZoom = false;
            options.OverviewRulerBorder = false;
            options.ScrollBeyondLastLine = false;
            options.FontLigatures = false;
            options.SnippetSuggestions = "none";
            options.CodeLens = false;
            options.QuickSuggestions = new QuickSuggestionsOptions { Comments = false, Other = false, Strings = false };
            options.Minimap = new EditorMinimapOptions { Enabled = false };
            options.Hover = new EditorHoverOptions { Enabled = false };
            options.MatchBrackets = "always";
            options.BracketPairColorization = new BracketPairColorizationOptions { Enabled = true };
            options.RenderLineHighlightOnlyWhenFocus = true;

            options.DiffCodeLens = false;
            options.RenderOverviewRuler = true;

            // Apply global user settings
            options.FontFamily = _settingsProvider.GetSetting(PredefinedSettings.TextEditorFont);
            options.WordWrap = _settingsProvider.GetSetting(PredefinedSettings.TextEditorTextWrapping) ? "on" : "off";
            options.LineNumbers = _settingsProvider.GetSetting(PredefinedSettings.TextEditorLineNumbers) ? "on" : "off";
            options.RenderLineHighlight = _settingsProvider.GetSetting(PredefinedSettings.TextEditorHighlightCurrentLine) ? "all" : "none";
            options.RenderWhitespace = _settingsProvider.GetSetting(PredefinedSettings.TextEditorRenderWhitespace) ? "all" : "none";

            // Create the bridges for the inner editors
            _originalEditor = MonacoEditorHelper.CreateVirtualEditor(JSRuntime, Id + "_original");
            _modifiedEditor = MonacoEditorHelper.CreateVirtualEditor(JSRuntime, Id + "_modified");

            Guard.IsNotNull(OriginalEditor);
            Guard.IsNotNull(ModifiedEditor);

            // Create the editor
            await MonacoEditorHelper.CreateMonacoEditorDiffInstanceAsync(
                JSRuntime,
                Id,
                options,
                null,
                Reference,
                OriginalEditor.Reference,
                ModifiedEditor.Reference);
        }

        await base.OnAfterRenderAsync(firstRender);

        if (IsActuallyEnabled != _oldIsActuallyEnabled)
        {
            _oldIsActuallyEnabled = IsActuallyEnabled;

            var options = new DiffEditorOptions();
            if (IsActuallyEnabled)
            {
                options.ReadOnly = _oldReadOnlyState;
            }
            else
            {
                Guard.IsNotNull(ModifiedEditor);
                EditorOptions currentOptions = await this.ModifiedEditor.GetRawOptionsAsync();
                _oldReadOnlyState = currentOptions.ReadOnly.GetValueOrDefault(false);
                options.ReadOnly = true;
            }

            await UpdateOptionsAsync(options);
        }
    }

    public override ValueTask DisposeAsync()
    {
        if (_themeListener is not null)
        {
            _themeListener.ThemeChanged -= ThemeListener_ThemeChanged;
        }

        if (_settingsProvider is not null)
        {
            _settingsProvider.SettingChanged -= SettingsProvider_SettingChanged;
        }

        return base.DisposeAsync();
    }

    protected override void OnEditorLoaded()
    {
        base.OnEditorLoaded();
        lock (_lock)
        {
            _isLoaded = true;
            ShowLoading = false;
            StateHasChanged();
        }
    }

    private void SettingsProvider_SettingChanged(object? sender, SettingChangedEventArgs e)
    {
        var options = new DiffEditorOptions()
        {
            FontFamily = _settingsProvider.GetSetting(PredefinedSettings.TextEditorFont),
            WordWrap = _settingsProvider.GetSetting(PredefinedSettings.TextEditorTextWrapping) ? "on" : "off",
            LineNumbers = _settingsProvider.GetSetting(PredefinedSettings.TextEditorLineNumbers) ? "on" : "off",
            RenderLineHighlight = _settingsProvider.GetSetting(PredefinedSettings.TextEditorHighlightCurrentLine) ? "all" : "none",
            RenderWhitespace = _settingsProvider.GetSetting(PredefinedSettings.TextEditorRenderWhitespace) ? "all" : "none"
        };

        UpdateOptionsAsync(options);
    }

    private void ThemeListener_ThemeChanged(object? sender, EventArgs e)
    {
        MonacoEditorHelper.SetThemeAsync(JSRuntime, GetTheme());
    }

    private string GetTheme()
    {
        Guard.IsNotNull(_themeListener);
        return _themeListener.ActualAppTheme == ApplicationTheme.Dark ? BuiltinTheme.VsDark : BuiltinTheme.Vs;
    }

    private async Task DisplayLoadingIfSlowLoadingAsync()
    {
        // Let's not show the progress ring during the first 100ms. We know customers tends to perceive anything
        // faster than 100ms as "instant", so no need to bother the user with a very shortly displayed progress ring
        // if we succeed to load within 100ms.
        // https://psychology.stackexchange.com/questions/1664/what-is-the-threshold-where-actions-are-perceived-as-instant

        const int Delay = 100;

        await Task.Delay(Delay);

        lock (_lock)
        {
            if (!_isLoaded)
            {
                ShowLoading = true;
                StateHasChanged();
            }
        }
    }
}