using Bit.Core.Tools.Repositories;
using Bit.Core.Vault.Models.Data;
using Bit.Core.Vault.Repositories;

namespace Bit.Core.Vault.Queries;

public class GetVaultHealthAnalysisQuery : IGetVaultHealthAnalysisQuery
{
    private readonly ICipherRepository _cipherRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly ISendRepository _sendRepository;

    public GetVaultHealthAnalysisQuery(
        ICipherRepository cipherRepository,
        IFolderRepository folderRepository,
        ISendRepository sendRepository)
    {
        _cipherRepository = cipherRepository;
        _folderRepository = folderRepository;
        _sendRepository = sendRepository;
    }

    public async Task<VaultHealthAnalysis> GetByUserIdAsync(Guid userId)
    {
        var ciphersTask = _cipherRepository.GetManyByUserIdAsync(userId);
        var foldersTask = _folderRepository.GetManyByUserIdAsync(userId);
        var sendsTask = _sendRepository.GetManyByUserIdAsync(userId);
        await Task.WhenAll(ciphersTask, foldersTask, sendsTask);

        var ciphers = ciphersTask.Result;
        var folders = foldersTask.Result;
        var sends = sendsTask.Result;

        return new VaultHealthAnalysis(
            totalItems: ciphers.Count,
            personalItems: ciphers.Count(cipher => cipher.OrganizationId == null),
            organizationItems: ciphers.Count(cipher => cipher.OrganizationId.HasValue),
            favoriteItems: ciphers.Count(cipher => cipher.Favorite),
            deletedItems: ciphers.Count(cipher => cipher.DeletedDate.HasValue),
            archivedItems: ciphers.Count(cipher => cipher.ArchivedDate.HasValue),
            folderCount: folders.Count,
            sendCount: sends.Count,
            analyzedAt: DateTime.UtcNow);
    }
}
