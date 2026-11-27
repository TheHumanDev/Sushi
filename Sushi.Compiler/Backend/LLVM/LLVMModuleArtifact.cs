using LLVMSharp.Interop;

namespace Sushi.Compiler.Backend.LLVM;

/// <summary>
/// Owns an LLVM module together with the context in which it was created.
/// </summary>
public sealed class LLVMModuleArtifact(LLVMContextRef context, LLVMModuleRef module) : IModuleArtifact
{
    private LLVMContextRef context = context;
    private LLVMModuleRef module = module;
    private bool disposed;

    /// <summary>
    /// Gets the generated LLVM module.
    /// </summary>
    public LLVMModuleRef Module
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            return this.module;
        }
    }

    /// <summary>
    /// Gets the LLVM context.
    /// </summary>
    public LLVMContextRef Context
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            return this.context;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.module.Dispose();
        this.module = default;

        this.context.Dispose();
        this.context = default;

        this.disposed = true;
    }
}
