using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using Sushi.LanguageServer.Handlers;
using LSP = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace Sushi.LanguageServer;

/// <inheritdoc cref="ISushiLanguageServer"/>
public sealed class SushiLanguageServer : ISushiLanguageServer
{
    /// <inheritdoc />
    public async Task Run(LSPTransportMethod transportMethod, IPEndPoint? listenEndpoint, string version)
    {
        switch (transportMethod)
        {
            case LSPTransportMethod.Stdio:
            {
                await using Stream input = Console.OpenStandardInput();
                await using Stream output = Console.OpenStandardOutput();
                await this.RunLanguageServer(input, output, version);
                break;
            }
            case LSPTransportMethod.TCP:
                await this.RunTCPLanguageServer(listenEndpoint, version);
                break;
            default:
                InvalidOperationException exception = new("Tried to run Language Server without a valid transport method.");
                Log.Error(exception, "Invalid LSP Transport {TransportMethod}", transportMethod);
                throw exception;
        }
    }

    /// <summary>
    /// Runs the language server with a <see cref="NetworkStream"/>, while set up to listen on localhost.
    /// </summary>
    /// <param name="listenEndpoint">
    /// The endpoint to listen on.
    /// </param>
    /// <param name="version">
    /// The version of the language server.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    private async Task RunTCPLanguageServer(IPEndPoint? listenEndpoint, string version)
    {
        IPEndPoint? endpoint = listenEndpoint;

        if (endpoint is null)
        {
            InvalidOperationException exception = new("TCP endpoint was not configured.");
            Log.Error(exception, "ListenEndpoint should not be null when running the LSP in TCP mode.");
            throw exception;
        }

        TcpListener listener = new(endpoint);

        try
        {
            listener.Start();

            Log.Information("Listening for LSP client on {Endpoint}.", endpoint);

            using TcpClient client = await listener.AcceptTcpClientAsync();
            await using NetworkStream stream = client.GetStream();

            await this.RunLanguageServer(stream, stream, version);
        }
        finally
        {
            listener.Stop();
            listener.Dispose();
        }
    }

    /// <summary>
    /// Runs the language server with the specified input and output streams.
    /// </summary>
    /// <param name="input">
    /// The input stream.
    /// </param>
    /// <param name="output">
    /// The output stream.
    /// </param>
    /// <param name="version">
    /// The version of the language server.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    private async Task RunLanguageServer(Stream input, Stream output, string version)
    {
        WorkspaceOrchestrator workspace = new();
        using LSP server = await LSP.From(options => options
            .WithInput(input)
            .WithOutput(output)
            .WithServerInfo(new ServerInfo
            {
                Name = "Sushi",
                Version = version
            })
            .OnInitialize((server, request, token) => workspace.Initialize([.. request.WorkspaceFolders ?? []], token))
            .OnStarted((server, token) => workspace.Start(server, token))
            .WithServices(services =>
            {
                services.AddSingleton(this);
                services.AddSingleton(workspace);
            })
            .WithHandler<WorkspaceFoldersHandler>()
        );

        await server.WaitForExit;
    }
}
