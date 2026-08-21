using EventHub.Tests.Fakes;

namespace EventHub.Tests.Fakes;

[TestFixture]
public class FakeUnitOfWorkTests
{
    [Test]
    public async Task ExecuteInTransaction_InvokesActionAndReturnsResult()
    {
        var uow = new FakeUnitOfWork();

        var result = await uow.ExecuteInTransactionAsync(async ct =>
        {
            await uow.SaveChangesAsync(ct);
            return 42;
        });

        Assert.That(result, Is.EqualTo(42));
        Assert.That(uow.SaveCalls, Is.EqualTo(1));
    }

    [Test]
    public void ExecuteInTransaction_PropagatesException()
    {
        var uow = new FakeUnitOfWork();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteInTransactionAsync<int>(_ =>
                throw new InvalidOperationException("boom")));
    }

    [Test]
    public async Task SaveChanges_IncrementsCallCount()
    {
        var uow = new FakeUnitOfWork();
        await uow.SaveChangesAsync();
        await uow.SaveChangesAsync();
        Assert.That(uow.SaveCalls, Is.EqualTo(2));
    }
}
