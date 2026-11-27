namespace Sushi.Compiler.Intermediate.Lowering;

public sealed class BlockBuilder(string name)
{
    private LoweredTerminator? terminator;

    public string Name { get; } = name;

    public List<LoweredInstruction> Instructions { get; } = [];

    public bool IsTerminated => this.terminator is not null;

    public void Terminate(LoweredTerminator value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (this.terminator is not null)
        {
            throw new InvalidOperationException($"Basic block \"{this.Name}\" already has a terminator.");
        }

        this.terminator = value;
    }

    public LoweredBasicBlock Complete()
        => new(this.Name, this.Instructions, this.terminator ?? throw new InvalidOperationException($"Basic block \"{this.Name}\" has no terminator."));
}
