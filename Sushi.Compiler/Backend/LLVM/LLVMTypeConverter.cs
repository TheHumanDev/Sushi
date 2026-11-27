using LLVMSharp.Interop;
using Sushi.Compiler.Intermediate;

namespace Sushi.Compiler.Backend.LLVM;

/// <summary>
/// Handles conversion of Sushi IR types to LLVM types.
/// </summary>
public static class LLVMTypeConverter
{
    /// <summary>
    /// Converts an <see cref="IRType" /> to its corresponding <see cref="LLVMTypeRef" />.
    /// </summary>
    /// <param name="context">
    /// The LLVM context used to reference the types.
    /// </param>
    /// <param name="type">
    /// The <see cref="IRType" /> to convert.
    /// </param>
    /// <returns>
    /// The corresponding <see cref="LLVMTypeRef" />.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if the <see cref="IRType"/> is not supported.
    /// </exception>
    public static LLVMTypeRef ConvertType(LLVMContextRef context, IRType type)
    => type switch
    {
        IRType.Int32 => context.Int32Type,
        IRType.Bool => context.Int1Type,
        IRType.Void => context.VoidType,
        _ => throw new NotSupportedException($"IR type \"{type}\" is not supported by the LLVM backend.")
    };
}
