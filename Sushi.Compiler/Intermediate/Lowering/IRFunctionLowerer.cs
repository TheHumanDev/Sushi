namespace Sushi.Compiler.Intermediate.Lowering;

/// <summary>
/// Lowers structured Sushi IR into explicit control flow while preserving
/// loops, nested loop transfers, checked arithmetic, local storage, integer
/// updates, and function-call argument order.
/// </summary>
public sealed class IRFunctionLowerer
{
    /// <summary>
    /// Converts a structured function into lowered control-flow representation.
    /// </summary>
    /// <param name="function">
    /// The structured function to lower.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancels lowering.
    /// </param>
    /// <returns>
    /// A function with typed SSA values, ordered basic blocks, and explicit edges.
    /// </returns>
    public static LoweredFunction Lower(IRFunction function, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(function);
        cancellationToken.ThrowIfCancellationRequested();

        LoweringContext context = new(cancellationToken);
        return context.Lower(function);
    }
}
