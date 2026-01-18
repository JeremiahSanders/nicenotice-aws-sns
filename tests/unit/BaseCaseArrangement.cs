using TUnit.Core.Interfaces;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

/// <summary>
///   A base type used for representing a test case arrangement.
/// </summary>
/// <remarks>
///   <para>TUnit will invoke <see cref="IAsyncInitializer.InitializeAsync" /> before tests.</para>
///   <para>
///     To prevent repeat invocation, this arrangement should be consumed with a
///     <see cref="ClassDataSourceAttribute" /> with <see cref="SharedType.PerTestSession" />
///     within a test (assertion) class.
///     (E.g., <c>[ClassDataSource&lt;TArrangement&gt;(Shared = SharedType.PerTestSession)]</c>)
///   </para>
///   <para>
///     <a href="https://tunit.dev/docs/advanced/extension-points/#iasyncinitializer">See TUnit documentation.</a>
///   </para>
/// </remarks>
public abstract class BaseCaseArrangement : IAsyncInitializer, IAsyncDisposable
{
  public virtual ValueTask DisposeAsync()
  {
    GC.SuppressFinalize(this);

    return ValueTask.CompletedTask;
  }

  async Task IAsyncInitializer.InitializeAsync()
  {
    await ArrangeAsync();
    await AcquireSanityValuesAsync();
    await ActAsync();
    await AcquireVerificationValuesAsync();
    await CleanupAsync();
  }

  protected virtual Task ArrangeAsync()
  {
    return Task.CompletedTask;
  }

  protected virtual Task ActAsync()
  {
    return Task.CompletedTask;
  }

  protected virtual Task AcquireSanityValuesAsync()
  {
    return Task.CompletedTask;
  }

  protected virtual Task AcquireVerificationValuesAsync()
  {
    return Task.CompletedTask;
  }

  protected virtual Task CleanupAsync()
  {
    return Task.CompletedTask;
  }
}
