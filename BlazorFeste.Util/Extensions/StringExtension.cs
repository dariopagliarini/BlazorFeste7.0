using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorFeste.Util
{
  public static class StringExtension
  {
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
          null => throw new ArgumentNullException(nameof(input)),
          "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
          _ => input[0].ToString().ToUpper() + input[1..]
        };
    public static string FirstCharToLower(this string input) =>
        input switch
        {
          null => throw new ArgumentNullException(nameof(input)),
          "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
          _ => input[0].ToString().ToLower() + input[1..]
        };
    public static string Truncate(this string input, int maxChars)
    {
      if (string.IsNullOrEmpty(input)) return string.Empty;
      return input.Substring(0, Math.Min(input.Length, maxChars));
    }
    public static string CR_to_Space(this string input)
    {
      if (string.IsNullOrEmpty(input)) return string.Empty;
      return input.Replace("<CR>", " ");
    }

    /// <summary>
    /// Returns a new string that center aligns the characters in a 
    /// string by padding them on the left and right with a specified 
    /// character, of a specified total length
    /// </summary>
    /// <param name="source">The source string</param>
    /// <param name="totalWidth">The number of characters to pad the source string</param>
    /// <param name="paddingChar">The padding character</param>
    /// <returns>The modified source string padded with as many paddingChar
    /// characters needed to create a length of totalWidth</returns>
    public static string PadCenter(this string source, int totalWidth, char paddingChar = ' ')
    {
      int spaces = totalWidth - source.Length;
      int padLeft = spaces / 2 + source.Length;
      return source.PadLeft(padLeft, paddingChar).PadRight(totalWidth, paddingChar);
    }
  }
}
