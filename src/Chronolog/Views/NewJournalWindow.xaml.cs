using System.Windows;
using Chronolog.Models;

namespace Chronolog.Views;

public partial class NewJournalWindow : Window
{
    public Journal? ResultJournal { get; private set; }

    public NewJournalWindow()
    {
        InitializeComponent();
        OwnerBox.Text = Environment.UserName;
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim() ?? string.Empty;
        var owner = OwnerBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(
                this,
                "Please enter a journal name.",
                "Chronolog",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            NameBox.Focus();
            return;
        }

        ResultJournal = new Journal
        {
            Name = name,
            Owner = owner,
            CreatedAt = DateTime.Now
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
