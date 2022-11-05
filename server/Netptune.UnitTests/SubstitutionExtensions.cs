using Netptune.Core.UnitOfWork;

using NSubstitute;

namespace Netptune.UnitTests;

public static class SubstitutionExtensions
{
    public static void InvokeTransaction(this INetptuneUnitOfWork unitOfWork)
    {
        unitOfWork.Transaction(Arg.Any<Func<Task>>())
            .Returns(x => x.Arg<Func<Task>>()
                .Invoke());
    }

    public static void InvokeTransaction<TResult>(this INetptuneUnitOfWork unitOfWork)
    {
        unitOfWork.Transaction(Arg.Any<Func<Task<TResult>>>())
            .Returns(x => x.Arg<Func<Task<TResult>>>()
                .Invoke());
    }
}
