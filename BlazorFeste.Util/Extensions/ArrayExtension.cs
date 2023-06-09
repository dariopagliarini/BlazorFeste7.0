﻿using System.Text;

namespace BlazorFeste.Util
{
  public static class ArrayExtension
  {
    //https://stackoverflow.com/questions/1822811/int-array-to-string
    public static string Print<T>(this T[] array, string delimiter)
    {
      if (array != null)
      {
        if (array.Length == 0)
          return string.Empty;
        if (array.Length == 1)
          return array[0].ToString();

        // determine if the length of the array is greater than the performance threshold for using a stringbuilder
        // 10 is just an arbitrary threshold value I've chosen
        if (array.Length < 10)
        {
          // assumption is that for arrays of less than 10 elements
          // this code would be more efficient than a StringBuilder.
          // Note: this is a crazy/pointless micro-optimization.  Don't do this.
          string[] values = new string[array.Length];

          for (int i = 0; i < values.Length; i++)
            values[i] = array[i].ToString();

          return string.Join(delimiter, values);
        }
        else
        {
          // for arrays of length 10 or longer, use a StringBuilder
          StringBuilder sb = new StringBuilder();

          sb.Append(array[0]);
          for (int i = 1; i < array.Length; i++)
          {
            sb.Append(delimiter);
            sb.Append(array[i]);
          }

          return sb.ToString();
        }
      }
      else
      {
        return null;
      }
    }
  }
}
