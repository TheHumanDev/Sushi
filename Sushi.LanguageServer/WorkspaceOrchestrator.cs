using System.Diagnostics.CodeAnalysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Sushi.LanguageServer;

/// <summary>
/// Handles the orchestration and plumbing of source document tracking.
/// </summary>
public sealed class WorkspaceOrchestrator
{
    /// <summary>
    /// The workspace folders from the client. Contains the sushi project files when running in LSP mode.
    /// </summary>
    private readonly List<WorkspaceFolder> folders = [];

    /// <summary>
    /// Initializes the language service with the information about the currently open workspace.
    /// </summary>
    /// <param name="workspaceFolders">
    /// The folders that are a part of the workspace.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    public Task Initialize([NotNull] IEnumerable<WorkspaceFolder> workspaceFolders, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspaceFolders);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the workspace orchestrator.
    /// </summary>
    /// <param name="languageServer">
    /// The language server facade used to interface with the protocol.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel startup.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task"/>.
    /// </returns>
    public Task Start([NotNull] ILanguageServerFacade languageServer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(languageServer);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the workspace folders with the current changesset from the changed workspace folders.
    /// </summary>
    /// <param name="addedFolders">
    /// A list of folders that were added.
    /// </param>
    /// <param name="removedFolders">
    /// A list of folders that were removed.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// An awaitable <see cref="Task" />.
    /// </returns>
    public async Task UpdateWorkspaceFolders([NotNull] IEnumerable<WorkspaceFolder> addedFolders, [NotNull] IEnumerable<WorkspaceFolder> removedFolders, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(addedFolders);
        ArgumentNullException.ThrowIfNull(removedFolders);

        foreach (WorkspaceFolder removedFolder in removedFolders)
        {
            Uri removedUri = removedFolder.Uri.ToUri();
            this.folders.RemoveAll(folder => folder.Uri.ToUri().IsSamePath(removedUri));
        }

        foreach (WorkspaceFolder addedFolder in addedFolders)
        {
            Uri addedUri = addedFolder.Uri.ToUri();
            bool alreadyExists = this.folders.Any(folder => folder.Uri.ToUri().IsSamePath(addedUri));

            if (!alreadyExists)
            {
                this.folders.Add(addedFolder);
            }
        }
    }
}
