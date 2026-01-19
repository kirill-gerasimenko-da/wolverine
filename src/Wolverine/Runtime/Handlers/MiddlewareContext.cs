using JasperFx.CodeGeneration.Model;

namespace Wolverine.Runtime.Handlers;

/// <summary>
/// Provides thread-local context for middleware-created variables during code generation.
/// This enables DI-registered services to be constructed using types created by middleware.
/// </summary>
public static class MiddlewareContext
{
    private static readonly AsyncLocal<IReadOnlyList<Variable>> _variables = new();

    /// <summary>
    /// Gets or sets the variables created by middleware in the current chain.
    /// </summary>
    public static IReadOnlyList<Variable> CurrentVariables
    {
        get => _variables.Value ?? Array.Empty<Variable>();
        set => _variables.Value = value;
    }
}
