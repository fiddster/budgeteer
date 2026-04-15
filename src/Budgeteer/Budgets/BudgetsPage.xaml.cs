using Budgeteer.Core.Domain;
using Budgeteer.Core.Services;

namespace Budgeteer.Budgets;

public partial class BudgetsPage : ContentPage
{
    private readonly IBudgetService _budgets;

    public BudgetsPage(IBudgetService budgets)
    {
        InitializeComponent();
        _budgets = budgets;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async Task RefreshAsync() =>
        BudgetsList.ItemsSource = await _budgets.GetAllAsync();

    private async void OnAddBudgetClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("budgets/edit");
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Budget budget })
            await Shell.Current.GoToAsync($"budgets/edit?budgetId={budget.Id}");
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Budget budget })
        {
            bool confirmed = await DisplayAlert("Delete Budget",
                $"Delete \"{budget.Name}\"?", "Delete", "Cancel");
            if (!confirmed) return;
            await _budgets.DeleteAsync(budget.Id);
            await RefreshAsync();
        }
    }
}
