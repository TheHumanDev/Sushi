using System.Net;

namespace Sushi.LanguageServer;

/// <summary>
/// Handles the orchestration of the language server protocol for Sushi.
/// </summary>
public interface ISushiLanguageServer
{
    /// <summary>
    /// Runs the language server.
    /// </summary>
    /// <param name="transportMethod">
    /// The transport method to use.
    /// </param>
    /// <param name="listenEndpoint">
    /// The endpoint to listen on.
    /// </param>
    /// <param name="version">
    /// The version of the language server.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    public Task Run(LSPTransportMethod transportMethod, IPEndPoint? listenEndpoint, string version);
}
