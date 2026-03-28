using Budgeteer.Core.Domain;
using Budgeteer.Core.Services;

namespace Budgeteer.Accounts;

public partial class AccountsPage : ContentPage
{
    private readonly IAccountService _accounts;

    public AccountsPage(IAccountService accounts)
    {
        InitializeComponent();
        _accounts = accounts;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AccountsList.ItemsSource = await _accounts.GetAllAsync();
    }

    private async void OnAddAccountClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("New Account", "Account name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var typeStr = await DisplayActionSheet("Account type", "Cancel", null,
            nameof(AccountType.Checking), nameof(AccountType.Savings), nameof(AccountType.CreditCard));
        if (typeStr is null or "Cancel") return;

        var type = Enum.Parse<AccountType>(typeStr);
        await _accounts.AddAsync(name, type, notes: null);
        AccountsList.ItemsSource = await _accounts.GetAllAsync();
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Account account })
            await Shell.Current.GoToAsync($"import/mapping?accountId={account.Id}");
    }
}
