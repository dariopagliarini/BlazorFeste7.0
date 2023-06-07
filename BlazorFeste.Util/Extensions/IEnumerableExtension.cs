using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlazorFeste.Util
{
  public static class IEnumerableExtension
  {
    public static IEnumerable<IEnumerable<T>> ChunkList<T>(this IEnumerable<T> data, int size)
    {
      return data
        .Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / size)
        .Select(x => x.Select(v => v.Value));
    }

    public static IEnumerable<string> Split(this string str, int n)
    {
      if (String.IsNullOrEmpty(str) || n < 1)
      {
        throw new ArgumentException();
      }
      for (int i = 0; i < str.Length; i += n)
      {
        yield return str.Substring(i, Math.Min(n, str.Length - i));
      }
    }
  }
}
