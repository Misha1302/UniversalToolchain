namespace BasicCore.Contracts;

/// <summary>
///     Typed backend metadata boundary passed through BasicCore pipeline notifications.
/// </summary>
public interface IBackendPipelineComponent
{
    string ComponentId { get; }
}
