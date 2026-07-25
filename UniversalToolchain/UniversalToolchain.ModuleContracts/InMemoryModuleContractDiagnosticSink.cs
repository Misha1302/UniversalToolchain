using System.Collections.ObjectModel;

namespace UniversalToolchain.ModuleContracts;

public sealed class InMemoryModuleContractDiagnosticSink : IModuleContractDiagnosticSink
{
    private readonly object _gate = new();
    private readonly List<ModuleContractPipelineDiagnosticBatch> _batches = [];

    public IReadOnlyList<ModuleContractPipelineDiagnosticBatch> Batches
    {
        get
        {
            lock (_gate)
                return new ReadOnlyCollection<ModuleContractPipelineDiagnosticBatch>(_batches.ToArray());
        }
    }

    public void Report(ModuleContractPipelineDiagnosticBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        lock (_gate)
            _batches.Add(batch);
    }
}
