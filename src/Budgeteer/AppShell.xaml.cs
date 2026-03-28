using Budgeteer.Import;

namespace Budgeteer;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("import/mapping", typeof(ImportColumnMappingPage));
        Routing.RegisterRoute("import/preview", typeof(ImportPreviewPage));
    }
}
