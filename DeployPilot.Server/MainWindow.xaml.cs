using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DeployPilot.Client;
using DeployPilot.Shared;

namespace DeployPilot.Server;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly ServerSettings _settings;
    private int _activeBuilds;
    private int _queuedBuilds;
    private int _modulesCount;
    private int _buildTemplatesCount;
    private string _statusMessage = "Ready.";
    private string _apiStatus = "API: not checked";
    private string _databaseStatus = "Database: not checked";
    private string _agentStatus = "Agents: waiting for telemetry";

    public ObservableCollection<string> ConsoleEvents { get; } = [];

    public ObservableCollection<ServerBuildCard> Builds { get; } = [];

    public int ActiveBuilds
    {
        get => _activeBuilds;
        private set => SetField(ref _activeBuilds, value);
    }

    public int QueuedBuilds
    {
        get => _queuedBuilds;
        private set => SetField(ref _queuedBuilds, value);
    }

    public int ModulesCount
    {
        get => _modulesCount;
        private set => SetField(ref _modulesCount, value);
    }

    public int BuildTemplatesCount
    {
        get => _buildTemplatesCount;
        private set => SetField(ref _buildTemplatesCount, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public string ApiStatus
    {
        get => _apiStatus;
        private set => SetField(ref _apiStatus, value);
    }

    public string DatabaseStatus
    {
        get => _databaseStatus;
        private set => SetField(ref _databaseStatus, value);
    }

    public string AgentStatus
    {
        get => _agentStatus;
        private set => SetField(ref _agentStatus, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        _settings = ServerSettings.Load();
        InitializeComponent();
        DataContext = this;
        ApiEndpointBox.Text = _settings.ApiBaseUrl;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshDashboardAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDashboardAsync();
    }

    private async void SeedDemoButton_Click(object sender, RoutedEventArgs e)
    {
        await RunApiActionAsync(async client =>
        {
            var result = await client.SeedDemoAsync();
            AddConsoleEvent(result.Message);
            await RefreshDashboardAsync();
        });
    }

    private async Task RefreshDashboardAsync()
    {
        await RunApiActionAsync(async client =>
        {
            StatusMessage = "Refreshing dashboard...";
            var health = await client.GetHealthAsync();
            var modules = await client.GetModulesAsync();
            var buildTemplates = await client.GetBuildTemplatesAsync();
            var builds = await client.GetBuildJobsAsync();

            ActiveBuilds = builds.Count(job => job.Status == BuildJobStatus.Running);
            QueuedBuilds = builds.Count(job => job.Status == BuildJobStatus.Queued);
            ModulesCount = modules.Count;
            BuildTemplatesCount = buildTemplates.Count;

            Builds.Clear();
            foreach (var build in builds.Take(6))
            {
                Builds.Add(ServerBuildCard.From(build));
            }

            ApiStatus = $"API: {health.Status} at {health.Time.ToLocalTime():T}";
            DatabaseStatus = "Database: provider configured";
            AgentStatus = ActiveBuilds > 0 || QueuedBuilds > 0 ? "Agents: builds pending or running" : "Agents: idle";
            StatusMessage = $"Dashboard refreshed. {modules.Count} module(s), {builds.Count} build(s).";
            AddConsoleEvent(StatusMessage);
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
            ApiStatus = "API: offline";
            StatusMessage = $"API connection failed: {ex.Message}";
            AddConsoleEvent(StatusMessage);
        }
    }

    private void AddConsoleEvent(string message)
    {
        ConsoleEvents.Insert(0, $"[{DateTime.Now:T}] {message}");
        while (ConsoleEvents.Count > 50)
        {
            ConsoleEvents.RemoveAt(ConsoleEvents.Count - 1);
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

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
