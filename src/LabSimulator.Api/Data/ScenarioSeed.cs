using LabSimulator.Api.Domain;

namespace LabSimulator.Api.Data;

/// <summary>
/// Rubric of the scenarios shipped with the simulator. Changing it requires a new migration,
/// which keeps every rubric change reviewed and versioned.
/// </summary>
internal static class ScenarioSeed
{
    public const string InfusionPumpCode = "infusion-pump";

    private const string Inspection = "inspection";
    private const string Configuration = "configuration";

    // TODO: confirm MaxPoints per step with the values used by BPAC_ScoreComponement.
    private const int DefaultMaxPoints = 100;

    public static readonly ScenarioStep[] InfusionPumpSteps =
    [
        Step("correct-area", Inspection, "Área del hospital", 1),
        Step("dirty-pump", Inspection, "Bomba de infusión sucia", 2),
        Step("damaged-power-cable", Inspection, "Cable de poder", 3),
        Step("power-cable-disconnected", Inspection, "Cable desconectado de la fuente de poder", 4),
        Step("power-source-disconnected", Inspection, "Fuente de poder desconectada", 5),
        Step("maintenance-sticker", Inspection, "Sticker de mantenimiento", 6),
        Step("infusion-set-installed", Configuration, "Set de infusión", 7),
        Step("purge", Configuration, "Purga", 8),
        Step("patient-document", Configuration, "Documento del paciente", 9),
        Step("correct-medication", Configuration, "Medicamento del paciente", 10),
        Step("line-configuration", Configuration, "Configuración de las líneas en la bomba de infusión", 11),
    ];

    private static ScenarioStep Step(string code, string stage, string name, int order) => new()
    {
        ScenarioCode = InfusionPumpCode,
        Code = code,
        Stage = stage,
        Name = name,
        MaxPoints = DefaultMaxPoints,
        DisplayOrder = order,
    };
}
