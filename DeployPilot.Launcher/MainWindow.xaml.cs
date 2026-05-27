using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DeployPilot.Client;
using DeployPilot.Shared;

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
        ArtifactEndpointBox.Text = _settings.ArtifactBaseUrl;
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
            _settings.ArtifactBaseUrl = ArtifactEndpointBox.Text.Trim();
            _settings.Save();

            var client = DeployPilotClientFactory.Create(_settings.ApiBaseUrl);
            await action(client);
        }
        catch (Exception ex)
        {
            StatusMessage = $"API connection failed: {ex.Message}";
        }
    }

    private async void ModuleActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LauncherModuleCard card })
        {
            return;
        }

        await RunApiActionAsync(async client =>
        {
            if (!card.HasUpdate)
            {
                StatusMessage = $"{card.Name} is already installed.";
                return;
            }

            await InstallModuleAsync(client, card);
        });
    }

    private async Task InstallModuleAsync(DeployPilotApiClient client, LauncherModuleCard card)
    {
        if (card.Artifact is null)
        {
            StatusMessage = $"{card.Name} has an update but no artifact is published yet.";
            return;
        }

        var plan = new ArtifactInstallPlanner().CreatePlan(
            card.Artifact,
            _settings.ArtifactBaseUrl,
            _settings.InstallRoot,
            card.Name,
            card.LatestVersion);

        Directory.CreateDirectory(Path.GetDirectoryName(plan.StagingPath) ?? ".");
        Directory.CreateDirectory(plan.InstallPath);

        card.ActionLabel = "Downloading...";
        card.Status = "Downloading artifact...";
        StatusMessage = $"Downloading {card.Name} {card.LatestVersion}...";

        await using (var destination = File.Create(plan.StagingPath))
        {
            var progress = new Progress<DownloadProgress>(download =>
            {
                card.UpdateDownloadProgress(download.BytesReceived, download.TotalBytes, download.BytesPerSecond);
            });

            await client.DownloadArtifactAsync(plan.ArtifactUri, destination, progress);
        }

        card.Status = "Validating integrity...";
        var isValid = await new IntegrityService().MatchesSha256Async(plan.StagingPath, card.Artifact.Sha256);
        if (!isValid)
        {
            card.ActionLabel = "Update";
            card.Status = "Integrity validation failed.";
            StatusMessage = $"{card.Name} failed integrity validation.";
            return;
        }

        card.Status = "Installing files...";
        if (Path.GetExtension(plan.StagingPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(plan.StagingPath, plan.InstallPath, overwriteFiles: true);
        }
        else
        {
            File.Copy(plan.StagingPath, Path.Combine(plan.InstallPath, card.Artifact.FileName), overwrite: true);
        }

        _settings.InstalledVersions[card.ModuleId] = card.LatestVersion;
        _settings.Save();

        card.Progress = 100;
        card.Status = "Installed and verified.";
        card.ActionLabel = "Open";
        StatusMessage = $"{card.Name} {card.LatestVersion} installed.";
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
