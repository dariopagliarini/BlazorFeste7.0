using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorFeste.Util
{
  public static class IntExtension
  {
    public static int LimitToRange(
        this int value, int inclusiveMinimum, int inclusiveMaximum)
    {
      if (value < inclusiveMinimum) { return inclusiveMinimum; }
      if (value > inclusiveMaximum) { return inclusiveMaximum; }
      return value;
    }
    public static int LimitToRange(
        this int? value, int inclusiveMinimum, int inclusiveMaximum, int defaultValue = 0)
    {
      if(!value.HasValue) { return defaultValue; }
      if (value.Value < inclusiveMinimum) { return inclusiveMinimum; }
      if (value.Value > inclusiveMaximum) { return inclusiveMaximum; }
      return value.Value;
    }
  }
}
