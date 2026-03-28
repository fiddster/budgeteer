using Budgeteer.Core.Import;

namespace Budgeteer.Import;

[QueryProperty(nameof(AccountId), "accountId")]
public partial class ImportPreviewPage : ContentPage
{
    private readonly ImportSession _session;
    private ImportWizardViewModel _vm = null!;

    public int AccountId { get; set; }

    public ImportPreviewPage(ImportSession session)
    {
        InitializeComponent();
        _session = session;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = _session.Wizard!;
        RefreshUI();
    }

    private void RefreshUI()
    {
        ValidList.ItemsSource = _vm.ValidRows;
        DuplicateList.ItemsSource = _vm.DuplicateRows;
        ErrorList.ItemsSource = _vm.ErrorRows;

        DuplicatesHeader.IsVisible = _vm.DuplicateRows.Count > 0;
        ErrorsHeader.IsVisible = _vm.ErrorRows.Count > 0;

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var total = _vm.ValidRows.Count(r => r.IsIncluded)
                  + _vm.DuplicateRows.Count(r => r.IsIncluded);
        SummaryLabel.Text = $"{total} transaction(s) selected to import";
    }

    private void OnApplyCorrectionClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: ErrorRowViewModel row })
        {
            _vm.ApplyCorrection(row);
            RefreshUI();
        }
    }

    private void OnSkipErrorClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: ErrorRowViewModel row })
        {
            _vm.SkipError(row);
            RefreshUI();
        }
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        await _vm.CommitAsync(AccountId);
        await DisplayAlert("Import complete", "Transactions imported successfully.", "OK");
        // Navigate back to accounts
        await Shell.Current.GoToAsync("//accounts");
    }
}
