using System.Diagnostics.CodeAnalysis;
using LLVMSharp.Interop;
using Sushi.Compiler.Backend.Native;
using LLVMNative = LLVMSharp.Interop.LLVM;

namespace Sushi.Compiler.Backend.LLVM;

/// <summary>
/// Emits verified LLVM modules as native object files for the compiler host.
/// </summary>
public sealed class LLVMObjectEmitter : IObjectEmitter
{
    /// <summary>
    /// Emits an LLVM module as a host-native object file.
    /// </summary>
    /// <param name="artifact">
    /// The owned LLVM module to emit.
    /// </param>
    /// <param name="outputPath">
    /// The destination object-file path.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel object emission.
    /// </param>
    /// <returns>
    /// The emitted native object artifact.
    /// </returns>
    public CompiledArtifact EmitForHost([NotNull] IModuleArtifact artifact, [NotNull] string outputPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentException.ThrowIfNullOrEmpty(outputPath);

        if (artifact is not LLVMModuleArtifact llvmArtifact)
        {
            throw new ArgumentException("non-LLVMModuleArtifact objects are not supported in LLVMObjectEmitter.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        string fullOutputPath = Path.GetFullPath(outputPath);
        string? directory = Path.GetDirectoryName(fullOutputPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        LLVMTargetMachineRef targetMachine = default;
        LLVMTargetDataRef targetData = default;
        LLVMModuleRef module = llvmArtifact.Module;

        try
        {
            targetMachine = LLVMNativeTarget.CreateHostTargetMachine();
            targetData = targetMachine.CreateTargetDataLayout();

            if (targetData.Handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("LLVM could not create the host target data layout.");
            }

            string targetTriple = targetMachine.Triple;

            if (string.IsNullOrWhiteSpace(targetTriple))
            {
                throw new InvalidOperationException("LLVM target machine did not provide a target triple.");
            }

            module.Target = targetTriple;
            module.DataLayout = targetData.StringRepresentation;

            VerifyModule(module);

            EmitObjectFile(targetMachine, module, fullOutputPath, cancellationToken);

            return new CompiledArtifact(fullOutputPath, targetTriple);
        }
        finally
        {
            DisposeTargetData(targetData);
            DisposeTargetMachine(targetMachine);
        }
    }

    /// <summary>
    /// Emits an object file.
    /// </summary>
    /// <param name="targetMachine">
    /// The target machine.
    /// </param>
    /// <param name="module">
    /// The module.
    /// </param>
    /// <param name="outputPath">
    /// The output path.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel object emission.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if LLVM was unable to emit a native object file.
    /// </exception>
    private static void EmitObjectFile(LLVMTargetMachineRef targetMachine, LLVMModuleRef module, string outputPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string temporaryPath = $"{outputPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            if (!targetMachine.TryEmitToFile(module, temporaryPath, LLVMCodeGenFileType.LLVMObjectFile, out string message))
            {
                throw new InvalidOperationException($"LLVM could not emit the native object file: {message}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            File.Move(temporaryPath, outputPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>
    /// Verifies that the module is valid.
    /// </summary>
    /// <param name="module">
    /// The module to verify.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if LLVM rejects the module.
    /// </exception>
    private static void VerifyModule(LLVMModuleRef module)
    {
        if (module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out string message))
        {
            return;
        }

        throw new InvalidOperationException($"LLVM rejected the target-configured module: {message}");
    }

    /// <summary>
    /// Disposes of target data.
    /// </summary>
    /// <param name="targetData">
    /// The target data to dispose of.
    /// </param>
    private static unsafe void DisposeTargetData(LLVMTargetDataRef targetData)
    {
        if (targetData.Handle != IntPtr.Zero)
        {
            LLVMNative.DisposeTargetData(targetData);
        }
    }

    /// <summary>
    /// Disposes the target machine.
    /// </summary>
    /// <param name="targetMachine">
    /// The target machine to dispose of.
    /// </param>
    private static unsafe void DisposeTargetMachine(LLVMTargetMachineRef targetMachine)
    {
        if (targetMachine.Handle != IntPtr.Zero)
        {
            LLVMNative.DisposeTargetMachine(targetMachine);
        }
    }
}
