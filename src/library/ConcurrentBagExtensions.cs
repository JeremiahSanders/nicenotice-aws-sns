using System.Collections.Concurrent;

namespace Jds.NiceNotice.Aws.Sns;

internal static class ConcurrentBagExtensions
{
  /// <summary>
  ///   Invokes <see cref="ConcurrentBag{T}.Add(T)" /> for each item in <paramref name="items" />.
  ///   By default, this runs serially. However, if <paramref name="maxDegreeOfParallelism" /> is greater than 1,
  ///   then the items will be added in parallel.
  /// </summary>
  /// <remarks>
  ///   This patches a gap in the base type.
  /// </remarks>
  /// <param name="bag"></param>
  /// <param name="items"></param>
  /// <param name="maxDegreeOfParallelism"></param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static ConcurrentBag<T> AddRange<T>(
    this ConcurrentBag<T> bag,
    IEnumerable<T> items,
    int maxDegreeOfParallelism = 1
  )
  {
    if (maxDegreeOfParallelism == 1)
    {
      foreach (T item in items)
      {
        bag.Add(item);
      }
    }
    else
    {
      Parallel.ForEach(
        items,
        new ParallelOptions
        {
          MaxDegreeOfParallelism = Math.Clamp(maxDegreeOfParallelism, min: 1, int.MaxValue)
        },
        bag.Add
      );
    }

    return bag;
  }
}