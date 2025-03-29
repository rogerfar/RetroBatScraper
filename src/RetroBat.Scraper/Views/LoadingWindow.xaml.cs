using System.Windows;

namespace RetroBatScraper.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    public String Status
    {
        get => (String)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(String), typeof(LoadingWindow), new("Loading..."));
}
