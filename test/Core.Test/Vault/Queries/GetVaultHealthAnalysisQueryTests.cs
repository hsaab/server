using Bit.Core.Entities;
using Bit.Core.Tools.Entities;
using Bit.Core.Tools.Repositories;
using Bit.Core.Vault.Entities;
using Bit.Core.Vault.Models.Data;
using Bit.Core.Vault.Queries;
using Bit.Core.Vault.Repositories;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.Vault.Queries;

[SutProviderCustomize]
public class GetVaultHealthAnalysisQueryTests
{
    [Theory, BitAutoData]
    public async Task GetByUserIdAsync_ReturnsAggregateCounts(
        SutProvider<GetVaultHealthAnalysisQuery> sutProvider,
        User user)
    {
        var beforeQuery = DateTime.UtcNow;
        var organizationId = Guid.NewGuid();
        var ciphers = new List<CipherDetails>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = null,
                Favorite = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = null,
                OrganizationId = organizationId,
                DeletedDate = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = null,
                OrganizationId = organizationId,
                Favorite = true,
                ArchivedDate = DateTime.UtcNow
            }
        };
        var folders = new List<Folder>
        {
            new() { Id = Guid.NewGuid(), UserId = user.Id },
            new() { Id = Guid.NewGuid(), UserId = user.Id }
        };
        var sends = new List<Send>
        {
            new() { Id = Guid.NewGuid(), UserId = user.Id }
        };

        sutProvider.GetDependency<ICipherRepository>()
            .GetManyByUserIdAsync(user.Id)
            .Returns(ciphers);
        sutProvider.GetDependency<IFolderRepository>()
            .GetManyByUserIdAsync(user.Id)
            .Returns(folders);
        sutProvider.GetDependency<ISendRepository>()
            .GetManyByUserIdAsync(user.Id)
            .Returns(sends);

        var result = await sutProvider.Sut.GetByUserIdAsync(user.Id);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(1, result.PersonalItems);
        Assert.Equal(2, result.OrganizationItems);
        Assert.Equal(2, result.FavoriteItems);
        Assert.Equal(1, result.DeletedItems);
        Assert.Equal(1, result.ArchivedItems);
        Assert.Equal(2, result.FolderCount);
        Assert.Equal(1, result.SendCount);
        Assert.InRange(result.AnalyzedAt, beforeQuery, DateTime.UtcNow);

        await sutProvider.GetDependency<ICipherRepository>()
            .Received(1)
            .GetManyByUserIdAsync(user.Id);
        await sutProvider.GetDependency<IFolderRepository>()
            .Received(1)
            .GetManyByUserIdAsync(user.Id);
        await sutProvider.GetDependency<ISendRepository>()
            .Received(1)
            .GetManyByUserIdAsync(user.Id);
    }

    [Theory, BitAutoData]
    public async Task GetByUserIdAsync_StartsRepositoryCallsBeforeAwaitingResults(
        SutProvider<GetVaultHealthAnalysisQuery> sutProvider,
        User user)
    {
        var ciphersCompletion = new TaskCompletionSource<ICollection<CipherDetails>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var foldersCompletion = new TaskCompletionSource<ICollection<Folder>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sendsCompletion = new TaskCompletionSource<ICollection<Send>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        sutProvider.GetDependency<ICipherRepository>()
            .GetManyByUserIdAsync(user.Id)
            .Returns(ciphersCompletion.Task);
        sutProvider.GetDependency<IFolderRepository>()
            .GetManyByUserIdAsync(user.Id)
            .Returns(foldersCompletion.Task);
        sutProvider.GetDependency<ISendRepository>()
            .GetManyByUserIdAsync(user.Id)
            .Returns(sendsCompletion.Task);

        var queryTask = sutProvider.Sut.GetByUserIdAsync(user.Id);

        _ = sutProvider.GetDependency<ICipherRepository>()
            .Received(1)
            .GetManyByUserIdAsync(user.Id);
        _ = sutProvider.GetDependency<IFolderRepository>()
            .Received(1)
            .GetManyByUserIdAsync(user.Id);
        _ = sutProvider.GetDependency<ISendRepository>()
            .Received(1)
            .GetManyByUserIdAsync(user.Id);
        Assert.False(queryTask.IsCompleted);

        ciphersCompletion.SetResult(new List<CipherDetails>());
        foldersCompletion.SetResult(new List<Folder>());
        sendsCompletion.SetResult(new List<Send>());

        var result = await queryTask;

        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.FolderCount);
        Assert.Equal(0, result.SendCount);
    }
}
