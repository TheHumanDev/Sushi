using System.Diagnostics.CodeAnalysis;
using Sushi.Compiler.Semantics;

namespace Sushi.Compiler.Intermediate.Lowering;

public sealed class LoweringContext
{
    private readonly CancellationToken cancellationToken;
    private readonly List<BlockBuilder> blocks = [];
    private readonly Dictionary<int, IRType> localTypes = [];
    private BlockBuilder current;
    private int nextBlockIndex;

    private IRType returnType;

    public LoweringContext(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        this.current = this.CreateBlock("entry");
    }

    public LoweredFunction Lower([NotNull] IRFunction function)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        this.returnType = function.ReturnType;

        if (function.ReturnType is IRType.Void)
        {
            this.LowerVoidBody(function.Body);
        }
        else
        {
            this.LowerValueReturningBody(function.Body);
        }

        LoweredBasicBlock[] loweredBlocks = [.. this.blocks.Select(block => block.Complete())];
        return new LoweredFunction(function.Accessibility, function.Name, function.ReturnType, function.Parameters, loweredBlocks);
    }

    private void LowerVoidBody(IRBlock body)
    {
        this.LowerStatements(body);

        if (!this.current.IsTerminated)
        {
            this.current.Terminate(new LoweredReturnTerminator());
        }
    }

    private void LowerValueReturningBody(IRBlock body)
    {
        this.LowerStatements(body);

        if (!this.current.IsTerminated)
        {
            throw new NotSupportedException("A value-returning IR function has a control-flow path that reaches its end without returning a value.");
        }
    }

    private void LowerStatements(IRBlock body)
    {
        foreach (IRStatement statement in body.Statements)
        {
            this.cancellationToken.ThrowIfCancellationRequested();

            if (this.current.IsTerminated)
            {
                break;
            }

            switch (statement)
            {
                case IRReturnStatement result:
                    this.LowerReturn(result);
                    break;
                case IRExitStatement exit:
                    this.LowerExit(exit);
                    break;
                case IRVoidCallStatement call:
                    this.LowerVoidCall(call);
                    break;
                default:
                    throw new NotSupportedException($"Statement type \"{statement.GetType().Name}\" is not supported by the current lowering subset.");
            }
        }
    }

    private void LowerVoidCall(IRVoidCallStatement call)
    {
        List<LoweredCallArgument> arguments = this.LowerCallArguments(call.Arguments);
        this.current.Instructions.Add(new LoweredVoidCallInstruction(call.Symbol, call.ParameterTypes, arguments));
    }

    private void LowerReturn(IRReturnStatement statement)
    {
        if (statement.Expression is null)
        {
            if (this.returnType is not IRType.Void)
            {
                throw new NotSupportedException("A value-returning IR function cannot contain a bare return.");
            }

            this.current.Terminate(new LoweredReturnTerminator());
            return;
        }

        if (this.returnType is IRType.Void)
        {
            throw new NotSupportedException("A void IR function cannot return a value.");
        }

        LoweredValue value = this.LowerExpression(statement.Expression);

        if (value.Type != this.returnType)
        {
            throw new InvalidOperationException("The returned IR value does not match the function's return type.");
        }

        this.current.Terminate(new LoweredReturnTerminator(value));
    }

    private LoweredValue LowerExpression(IRExpression expression)
    {
        this.cancellationToken.ThrowIfCancellationRequested();

        return expression switch
        {
            _ => throw new NotSupportedException($"Structured IR expression type \"{expression.GetType().Name}\" is not supported by lowering.")
        };
    }

    private List<LoweredCallArgument> LowerCallArguments(IReadOnlyList<IRCallArgument> arguments)
    {
        List<LoweredCallArgument> lowered = [with(arguments.Count)];

        foreach (IRCallArgument argument in arguments)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            LoweredValue value = this.LowerExpression(argument.Expression);
            lowered.Add(new LoweredCallArgument(argument.ParameterIndex, value));
        }

        return lowered;
    }

    private void LowerExit(IRExitStatement statement)
    {
        LoweredValue status = statement.StatusExpression is null
            ? new LoweredIntegerConstant(ProgramTerminationCodes.GracefulSuccess)
            : this.LowerExpression(statement.StatusExpression);

        if (status.Type is not IRType.Int32)
        {
            throw new InvalidOperationException("A program exit status must have type int32.");
        }

        this.current.Terminate(new LoweredExitTerminator(status));
    }

    private BlockBuilder CreateBlock(string prefix)
    {
        string name = this.blocks.Count == 0 ? "entry" : $"{prefix}.{this.nextBlockIndex++}";
        BlockBuilder block = new(name);

        this.blocks.Add(block);
        return block;
    }
}
