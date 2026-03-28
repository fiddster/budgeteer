using Budgeteer.Core.Import;

namespace Budgeteer.Import;

/// <summary>Singleton that carries the active ImportWizardViewModel across wizard pages.</summary>
public class ImportSession
{
    public ImportWizardViewModel? Wizard { get; set; }
}
