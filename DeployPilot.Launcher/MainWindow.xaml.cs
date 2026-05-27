using System.Windows;
using System.Windows.Controls;

namespace DeployPilot.Launcher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
