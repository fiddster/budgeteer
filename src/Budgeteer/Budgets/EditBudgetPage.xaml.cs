using Budgeteer.Core.Domain;
using Budgeteer.Core.Services;

namespace Budgeteer.Budgets;

[QueryProperty(nameof(BudgetId), "budgetId")]
public partial class EditBudgetPage : ContentPage
{
    private readonly IBudgetService _budgets;
    private readonly ICategoryService _categories;
    private List<Category> _allCategories = [];
    private List<int> _selectedCategoryIds = [];

    public int BudgetId { get; set; }

    public EditBudgetPage(IBudgetService budgets, ICategoryService categories)
    {
        InitializeComponent();
        _budgets = budgets;
        _categories = categories;

        TimespanPicker.ItemsSource = Enum.GetValues<BudgetTimespan>().ToList();
        TimespanPicker.SelectedIndex = 1; // Monthly default
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _allCategories = [.. await _categories.GetAllAsync()];
        CategoriesList.ItemsSource = _allCategories;

        if (BudgetId > 0)
        {
            var all = await _budgets.GetAllAsync();
            var budget = all.FirstOrDefault(b => b.Id == BudgetId);
            if (budget is null) return;

            NameEntry.Text = budget.Name;
            LimitEntry.Text = budget.SpendingLimit.ToString("F2");
            TimespanPicker.SelectedItem = budget.Timespan;
            RolloverSwitch.IsToggled = budget.Rollover;
            AlertSwitch.IsToggled = budget.AlertEnabled;
            ThresholdLabel.IsVisible = budget.AlertEnabled;
            ThresholdEntry.IsVisible = budget.AlertEnabled;
            ThresholdEntry.Text = budget.AlertThresholdPercent?.ToString() ?? string.Empty;

            _selectedCategoryIds = budget.BudgetCategories.Select(bc => bc.CategoryId).ToList();
            var selectedItems = _allCategories.Where(c => _selectedCategoryIds.Contains(c.Id)).ToList();
            foreach (var item in selectedItems)
                CategoriesList.SelectedItems?.Add(item);
        }
    }

    private void OnAlertSwitchToggled(object sender, ToggledEventArgs e)
    {
        ThresholdLabel.IsVisible = e.Value;
        ThresholdEntry.IsVisible = e.Value;
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCategoryIds = e.CurrentSelection
            .OfType<Category>()
            .Select(c => c.Id)
            .ToList();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert("Validation", "Name is required.", "OK");
            return;
        }

        if (!decimal.TryParse(LimitEntry.Text, out var limit) || limit < 0)
        {
            await DisplayAlert("Validation", "Enter a valid spending limit.", "OK");
            return;
        }

        var timespan = TimespanPicker.SelectedItem is BudgetTimespan ts ? ts : BudgetTimespan.Monthly;
        bool rollover = RolloverSwitch.IsToggled;
        bool alertEnabled = AlertSwitch.IsToggled;
        int? threshold = null;
        if (alertEnabled && int.TryParse(ThresholdEntry.Text, out var t))
            threshold = t;

        if (BudgetId > 0)
            await _budgets.UpdateAsync(BudgetId, name, limit, timespan, rollover, alertEnabled, threshold, _selectedCategoryIds);
        else
            await _budgets.AddAsync(name, limit, timespan, rollover, alertEnabled, threshold, _selectedCategoryIds);

        await Shell.Current.GoToAsync("..");
    }
}
