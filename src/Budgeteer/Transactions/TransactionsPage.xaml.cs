using Budgeteer.Core.Domain;
using Budgeteer.Core.Services;

namespace Budgeteer.Transactions;

public partial class TransactionsPage : ContentPage
{
    private readonly ITransactionService _transactions;
    private readonly IAccountService _accounts;
    private readonly ICategoryService _categories;

    private List<Account> _allAccounts = [];
    private List<Category> _allCategories = [];

    public TransactionsPage(ITransactionService transactions, IAccountService accounts, ICategoryService categories)
    {
        InitializeComponent();
        _transactions = transactions;
        _accounts = accounts;
        _categories = categories;

        FromDate.Date = DateTime.Today.AddMonths(-1);
        ToDate.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _allAccounts = [.. await _accounts.GetAllAsync()];
        AccountFilter.ItemsSource = _allAccounts;
        AccountFilter.ItemDisplayBinding = new Binding("Name");

        _allCategories = [.. await _categories.GetAllAsync()];
        CategoryFilter.ItemsSource = _allCategories;
        CategoryFilter.ItemDisplayBinding = new Binding("Name");

        await ApplyFiltersAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        int? accountId = AccountFilter.SelectedIndex >= 0
            ? _allAccounts[AccountFilter.SelectedIndex].Id
            : null;

        int? categoryId = CategoryFilter.SelectedIndex >= 0
            ? _allCategories[CategoryFilter.SelectedIndex].Id
            : null;

        var from = DateOnly.FromDateTime(FromDate.Date);
        var to = DateOnly.FromDateTime(ToDate.Date);

        TransactionsList.ItemsSource = await _transactions.GetAllAsync(accountId, categoryId, from, to);
    }

    private async void OnAccountFilterChanged(object sender, EventArgs e) => await ApplyFiltersAsync();
    private async void OnCategoryFilterChanged(object sender, EventArgs e) => await ApplyFiltersAsync();
    private async void OnFromDateSelected(object sender, DateChangedEventArgs e) => await ApplyFiltersAsync();
    private async void OnToDateSelected(object sender, DateChangedEventArgs e) => await ApplyFiltersAsync();

    private async void OnClearFiltersClicked(object sender, EventArgs e)
    {
        AccountFilter.SelectedIndex = -1;
        CategoryFilter.SelectedIndex = -1;
        FromDate.Date = DateTime.Today.AddMonths(-1);
        ToDate.Date = DateTime.Today;
        await ApplyFiltersAsync();
    }

    private async void OnToggleTransferClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Transaction t })
        {
            await _transactions.MarkAsTransferAsync(t.Id, !t.IsTransfer);
            await ApplyFiltersAsync();
        }
    }
}
