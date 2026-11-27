using LLVMSharp.Interop;
using Sushi.Compiler.Intermediate;
using Sushi.Compiler.Intermediate.Lowering;

namespace Sushi.Compiler.Backend.LLVM;

/// <summary>
/// Translates lowered Sushi functions into verified LLVM IR.
/// </summary>
public sealed class LLVMModuleEmitter : IModuleEmitter
{
    /// <inheritdoc />
    public IModuleArtifact Emit(string moduleName, LoweredFunction function, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(function);
        return this.Emit(moduleName, [function], cancellationToken);
    }

    /// <inheritdoc />
    public IModuleArtifact Emit(string moduleName, IReadOnlyList<LoweredFunction> functions, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(moduleName);
        ArgumentNullException.ThrowIfNull(functions);

        if (functions.Count == 0)
        {
            throw new ArgumentException("A module must contain at least one function.", nameof(functions));
        }

        HashSet<string> symbols = [with(StringComparer.Ordinal)];

        foreach (LoweredFunction function in functions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(function);

            if (!symbols.Add(function.Name))
            {
                throw new ArgumentException($"Function symbol \"{function.Name}\" is duplicated.", nameof(functions));
            }
        }

        LLVMContextRef context = LLVMContextRef.Create();
        LLVMModuleRef module = default;

        try
        {
            module = context.CreateModuleWithName(moduleName);

            using LLVMBuilderRef builder = context.CreateBuilder();

            foreach (LoweredFunction function in functions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DeclareFunction(module, function);
            }

            if (functions.Any(function => function.Blocks.Any(block => block.Terminator is LoweredExitTerminator)))
            {
                DeclareTerminationFunctions(module);
            }

            foreach (LoweredFunction function in functions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                EmitFunction(module, builder, function, cancellationToken);
            }

            VerifyModule(module);
            return new LLVMModuleArtifact(context, module);
        }
        catch
        {
            if (module.Handle != IntPtr.Zero)
            {
                module.Dispose();
            }

            context.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Declares a function.
    /// </summary>
    /// <param name="module">
    /// The module to declare the function in.
    /// </param>
    /// <param name="function">
    /// The Sushi lowered IR function.
    /// </param>
    private static void DeclareFunction(LLVMModuleRef module, LoweredFunction function)
    {
        LLVMTypeRef returnType = LLVMTypeConverter.ConvertType(module.Context, function.ReturnType);
        LLVMTypeRef[] parameterTypes = [.. function.Parameters.Select(parameter => LLVMTypeConverter.ConvertType(module.Context, parameter.Type))];
        LLVMTypeRef functionType = LLVMTypeRef.CreateFunction(returnType, parameterTypes);
        LLVMValueRef llvmFunction = module.AddFunction(function.Name, functionType);

        foreach (IRParameter parameter in function.Parameters)
        {
            llvmFunction.GetParam((uint)parameter.Index).Name = parameter.Name;
        }
    }

    /// <summary>
    /// Declares termination functions.
    /// </summary>
    /// <param name="module">
    /// The module to declare the functions in.
    /// </param>
    private static void DeclareTerminationFunctions(LLVMModuleRef module)
    {
        LLVMContextRef context = module.Context;
        LLVMTypeRef functionType = LLVMTypeRef.CreateFunction(context.VoidType, [context.Int32Type]);
        module.AddFunction("exit", functionType);
        module.AddFunction("_Exit", functionType);
    }

    /// <summary>
    /// Emits a function.
    /// </summary>
    /// <param name="module">
    /// The module to emit the function into.
    /// </param>
    /// <param name="builder">
    /// The builder.
    /// </param>
    /// <param name="function">
    /// The Sushi lowered IR function.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel emission.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the function was not declared before emission.
    /// </exception>
    private static void EmitFunction(LLVMModuleRef module, LLVMBuilderRef builder, LoweredFunction function, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LLVMValueRef llvmFunction = module.GetNamedFunction(function.Name);

        if (llvmFunction.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Function \"{function.Name}\" was not declared before emission.");
        }

        LLVMFunctionEmitter emitter = new(module, llvmFunction, function, builder, cancellationToken);
        emitter.Emit();
    }

    /// <summary>
    /// Verifies the module.
    /// </summary>
    /// <param name="module">
    /// The module to verify.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when LLVM rejects the module.
    /// </exception>
    private static void VerifyModule(LLVMModuleRef module)
    {
        if (module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out string message))
        {
            return;
        }

        throw new InvalidOperationException($"LLVM rejected the generated module: {message}");
    }
}
