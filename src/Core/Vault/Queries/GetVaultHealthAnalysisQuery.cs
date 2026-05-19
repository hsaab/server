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
        var ciphers = await _cipherRepository.GetManyByUserIdAsync(userId);
        var folders = await _folderRepository.GetManyByUserIdAsync(userId);
        var sends = await _sendRepository.GetManyByUserIdAsync(userId);

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
