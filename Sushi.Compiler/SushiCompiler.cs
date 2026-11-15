namespace Sushi.Compiler;

/// <inheritdoc cref="ISushiCompiler"/>
public sealed class SushiCompiler : ISushiCompiler
{
    /// <inheritdoc />
    public Task CompileJob() => Task.CompletedTask;
}
