using Budgeteer.Core.Import;
using Budgeteer.Core.Services;
using System.Text;

namespace Budgeteer.Import;

[QueryProperty(nameof(AccountId), "accountId")]
public partial class ImportColumnMappingPage : ContentPage
{
    private readonly ICsvImportService _importService;
    private readonly IAccountService _accountService;
    private readonly ImportSession _session;

    private static readonly (string Label, char Value)[] DelimiterOptions =
    [
        ("Comma  (,)",     ','),
        ("Semicolon  (;)", ';'),
        ("Tab",            '\t'),
        ("Pipe  (|)",      '|'),
    ];

    private static readonly (string Label, string CodePage)[] EncodingOptions =
    [
        ("UTF-8",            "utf-8"),
        ("Windows-1252 (ANSI)", "windows-1252"),
    ];

    private int _accountId;
    private string? _csvContent;
    private bool _suppressEvents;

    public int AccountId
    {
        get => _accountId;
        set { _accountId = value; }
    }

    public ImportColumnMappingPage(
        ICsvImportService importService,
        IAccountService accountService,
        ImportSession session)
    {
        InitializeComponent();
        _importService = importService;
        _accountService = accountService;
        _session = session;

        DelimiterPicker.ItemsSource = DelimiterOptions.Select(d => d.Label).ToList();
        EncodingPicker.ItemsSource = EncodingOptions.Select(e => e.Label).ToList();
    }

    private async void OnPickFileClicked(object sender, EventArgs e)
    {
        var options = new PickOptions
        {
            PickerTitle = "Select a CSV file",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, [".csv"] },
                { DevicePlatform.macOS, ["csv"] },
            })
        };

        var result = await FilePicker.Default.PickAsync(options);
        if (result is null) return;

        // Detect encoding from BOM; default to Windows-1252 when no BOM found
        // (most bank CSV exports are Windows-1252 without BOM)
        string detectedCodePage;
        using (var peekStream = await result.OpenReadAsync())
        {
            var bom = new byte[3];
            var read = peekStream.Read(bom, 0, 3);
            detectedCodePage = (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                ? "utf-8"
                : "windows-1252";
        }

        SelectEncodingPicker(detectedCodePage);
        await ReloadFileWithEncoding(result, detectedCodePage);

        FileNameLabel.Text = result.FileName;
        MappingSection.IsVisible = true;
    }

    private async Task ReloadFileWithEncoding(FileResult result, string codePage)
    {
        var encoding = codePage == "windows-1252"
            ? System.Text.Encoding.GetEncoding(1252)
            : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        using var stream = await result.OpenReadAsync();
        using var reader = new StreamReader(stream, encoding);
        _csvContent = await reader.ReadToEndAsync();

        var detectedDelimiter = _importService.DetectDelimiter(_csvContent);
        SelectDelimiterPicker(detectedDelimiter);
        PopulateColumnPickers(detectedDelimiter);

        // Pre-fill from saved mapping if available
        var saved = await _accountService.GetColumnMappingAsync(_accountId);
        if (saved is not null)
        {
            SetPickerSelection(DatePicker, saved.DateColumn);
            SetPickerSelection(DescriptionPicker, saved.DescriptionColumn);
            SetPickerSelection(AmountPicker, saved.AmountColumn);
            SetPickerSelection(BalancePicker, saved.BalanceColumn);
            SetPickerSelection(ReferencePicker, saved.ReferenceColumn);
        }
    }

    private void OnDelimiterChanged(object sender, EventArgs e)
    {
        if (_suppressEvents || _csvContent is null) return;
        if (DelimiterPicker.SelectedIndex < 0) return;

        var delimiter = DelimiterOptions[DelimiterPicker.SelectedIndex].Value;
        PopulateColumnPickers(delimiter);
    }

    private void OnEncodingChanged(object sender, EventArgs e)
    {
        // Re-reading the file requires the FileResult — encoding is applied on next pick.
        // This picker is mainly for persistence; changing it live would need a stored FileResult.
    }

    private void SelectDelimiterPicker(char delimiter)
    {
        _suppressEvents = true;
        var idx = Array.FindIndex(DelimiterOptions, d => d.Value == delimiter);
        DelimiterPicker.SelectedIndex = idx >= 0 ? idx : 0;
        _suppressEvents = false;
    }

    private void SelectEncodingPicker(string codePage)
    {
        _suppressEvents = true;
        var idx = Array.FindIndex(EncodingOptions, e => e.CodePage == codePage);
        EncodingPicker.SelectedIndex = idx >= 0 ? idx : 0;
        _suppressEvents = false;
    }

    private void PopulateColumnPickers(char delimiter)
    {
        if (_csvContent is null) return;
        var headers = _importService.ParseHeaders(_csvContent, delimiter);
        var headersWithBlank = new[] { "" }.Concat(headers).ToList();

        DatePicker.ItemsSource = headers.ToList();
        DescriptionPicker.ItemsSource = headers.ToList();
        AmountPicker.ItemsSource = headers.ToList();
        BalancePicker.ItemsSource = headersWithBlank;
        ReferencePicker.ItemsSource = headersWithBlank;
    }

    private static void SetPickerSelection(Picker picker, string? value)
    {
        if (value is null) return;
        var idx = ((System.Collections.IList?)picker.ItemsSource)?.IndexOf(value) ?? -1;
        if (idx >= 0) picker.SelectedIndex = idx;
    }

    private async void OnParseClicked(object sender, EventArgs e)
    {
        if (_csvContent is null) return;
        if (DatePicker.SelectedItem is null || DescriptionPicker.SelectedItem is null || AmountPicker.SelectedItem is null)
        {
            await DisplayAlert("Missing columns", "Date, Description and Amount are required.", "OK");
            return;
        }

        var delimiter = DelimiterPicker.SelectedIndex >= 0
            ? DelimiterOptions[DelimiterPicker.SelectedIndex].Value
            : ',';

        var codePage = EncodingPicker.SelectedIndex >= 0
            ? EncodingOptions[EncodingPicker.SelectedIndex].CodePage
            : "utf-8";

        var savedMapping = await _accountService.GetColumnMappingAsync(_accountId);
        var vm = new ImportWizardViewModel(_importService, Array.Empty<Core.Domain.Transaction>(), savedMapping);
        vm.LoadCsv(_csvContent);
        vm.Delimiter = delimiter;
        vm.DateColumn = DatePicker.SelectedItem?.ToString();
        vm.DescriptionColumn = DescriptionPicker.SelectedItem?.ToString();
        vm.AmountColumn = AmountPicker.SelectedItem?.ToString();
        vm.BalanceColumn = BalancePicker.SelectedItem is string b && b.Length > 0 ? b : null;
        vm.ReferenceColumn = ReferencePicker.SelectedItem is string r && r.Length > 0 ? r : null;
        vm.Encoding = codePage;
        vm.Parse(_accountId);

        _session.Wizard = vm;
        await Shell.Current.GoToAsync($"import/preview?accountId={_accountId}");
    }
}
