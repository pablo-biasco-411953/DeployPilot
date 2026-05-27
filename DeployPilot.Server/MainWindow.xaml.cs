using System.Windows;
using System.Windows.Controls;

namespace DeployPilot.Server;

public partial class MainWindow : Window
{
    public IReadOnlyList<string> ConsoleEvents { get; } =
    [
        "[orchestrator] Build queue initialized",
        "[agent] Windows build agent online",
        "[artifacts] HTTP artifact root ready",
        "[api] Waiting for first setup",
        "[diagnostics] Database provider not configured yet"
    ];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageSelector.SelectedItem is not ComboBoxItem item || item.Tag is not string culture)
        {
            return;
        }

        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"Localization/Strings.{culture}.xaml", UriKind.Relative)
        };

        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dictionary);
    }
}
