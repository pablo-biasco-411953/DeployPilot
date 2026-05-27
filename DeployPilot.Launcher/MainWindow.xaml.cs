using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DeployPilot.Client;

namespace DeployPilot.Launcher;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly LauncherSettings _settings;
    private string _statusMessage = "Ready.";

    public ObservableCollection<LauncherModuleCard> Modules { get; } = [];

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        _settings = LauncherSettings.Load();
        InitializeComponent();
        DataContext = this;
        ApiEndpointBox.Text = _settings.ApiBaseUrl;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshModulesAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshModulesAsync();
    }

    private async void SeedDemoButton_Click(object sender, RoutedEventArgs e)
    {
        await RunApiActionAsync(async client =>
        {
            var result = await client.SeedDemoAsync();
            StatusMessage = result.Message;
            await RefreshModulesAsync();
        });
    }

    private async Task RefreshModulesAsync()
    {
        await RunApiActionAsync(async client =>
        {
            StatusMessage = "Checking modules...";
            Modules.Clear();

            var modules = await client.GetModulesAsync();
            foreach (var module in modules)
            {
                var currentVersion = _settings.InstalledVersions.TryGetValue(module.Id, out var installedVersion)
                    ? installedVersion
                    : "0.0.0";
                var update = await client.CheckForUpdateAsync(module.Id, currentVersion);
                Modules.Add(LauncherModuleCard.From(module, currentVersion, update));
            }

            StatusMessage = modules.Count == 0
                ? "No modules were found. Seed demo data or connect to a configured DeployPilot API."
                : $"Loaded {modules.Count} module(s).";
        });
    }

    private async Task RunApiActionAsync(Func<DeployPilotApiClient, Task> action)
    {
        try
        {
            _settings.ApiBaseUrl = ApiEndpointBox.Text.Trim();
            _settings.Save();

            var client = DeployPilotClientFactory.Create(_settings.ApiBaseUrl);
            await action(client);
        }
        catch (Exception ex)
        {
            StatusMessage = $"API connection failed: {ex.Message}";
        }
    }

    private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageSelector.SelectedItem is not ComboBoxItem item || item.Tag is not string culture)
        {
            return;
        }

        _settings.Language = culture;
        _settings.Save();

        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"Localization/Strings.{culture}.xaml", UriKind.Relative)
        };

        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dictionary);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
