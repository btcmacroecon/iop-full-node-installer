using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FullNodeInstaller
{
  /// <summary>
  /// Implements routines for reading inputs from console and writing ouputs to console. 
  /// </summary>
  public static class CUI
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("FullNodeInstaller.CUI");

    /// <summary>Foreground color of the console when the program started.</summary>
    private static ConsoleColor originalForegroundColor;

    /// <summary>Background color of the console when the program started.</summary>
    private static ConsoleColor originalBackgroundColor;


    /// <summary>
    /// Initializes the console.
    /// </summary>
    public static void InitConsole()
    {
      Console.InputEncoding = Encoding.UTF8;
      Console.OutputEncoding = Encoding.UTF8;
      SaveOriginalColorScheme();
    }


    /// <summary>
    /// Finalizes the work with the console, restores its colors.
    /// </summary>
    public static void FinitConsole()
    {
      RestoreOriginalColorScheme();
    }


    /// <summary>
    /// Saves console color scheme so that it can be restored using RestoreOriginalColorScheme.
    /// </summary>
    public static void SaveOriginalColorScheme()
    {
      originalForegroundColor = Console.ForegroundColor;
      originalBackgroundColor = Console.BackgroundColor;
    }

    /// <summary>
    /// Restores console color scheme.
    /// </summary>
    public static void RestoreOriginalColorScheme()
    {
      Console.ForegroundColor = originalForegroundColor;
      Console.BackgroundColor = originalBackgroundColor;
    }

    /// <summary>
    /// Writes an empty line to the console output.
    /// </summary>
    public static void WriteLine()
    {
      Console.WriteLine();
    }

    /// <summary>
    /// Writes to the console output.
    /// </summary>
    /// <param name="Format">Composite format string.</param>
    /// <param name="Args">Array of objects to write using format.</param>
    public static void Write(string Format, params object[] Args)
    {
      Console.Write(Format, Args);
    }



    /// <summary>
    /// Writes a line to the console output.
    /// </summary>
    /// <param name="Format">Composite format string.</param>
    /// <param name="Args">Array of objects to write using format.</param>
    public static void WriteLine(string Format, params object[] Args)
    {
      Console.WriteLine(Format, Args);
    }


    /// <summary>
    /// Writes to the console output with the specified foreground color.
    /// </summary>
    /// <param name="FgColor">Foreground color to use.</param>
    /// <param name="Format">Composite format string.</param>
    /// <param name="Args">Array of objects to write using format.</param>
    public static void Write(ConsoleColor FgColor, string Format, params object[] Args)
    {
      ConsoleColor orgColor = Console.ForegroundColor;
      Console.ForegroundColor = FgColor;
      Console.Write(Format, Args);
      Console.ForegroundColor = orgColor;
    }

    /// <summary>
    /// Writes a line to the console output with the specified foreground color.
    /// </summary>
    /// <param name="FgColor">Foreground color to use.</param>
    /// <param name="Format">Composite format string.</param>
    /// <param name="Args">Array of objects to write using format.</param>
    public static void WriteLine(ConsoleColor FgColor, string Format, params object[] Args)
    {
      ConsoleColor orgColor = Console.ForegroundColor;
      Console.ForegroundColor = FgColor;
      Console.WriteLine(Format, Args);
      Console.ForegroundColor = orgColor;
    }


    /// <summary>
    /// Writes to the console output with the specified foreground and background colors.
    /// </summary>
    /// <param name="FgColor">Foreground color to use.</param>
    /// <param name="BgColor">Background color to use.</param>
    /// <param name="Format">Composite format string.</param>
    /// <param name="Args">Array of objects to write using format.</param>
    public static void Write(ConsoleColor FgColor, ConsoleColor BgColor, string Format, params object[] Args)
    {
      ConsoleColor orgFgColor = Console.ForegroundColor;
      ConsoleColor orgBgColor = Console.ForegroundColor;
      Console.ForegroundColor = FgColor;
      Console.BackgroundColor = BgColor;
      Console.Write(Format, Args);
      Console.ForegroundColor = orgFgColor;
      Console.BackgroundColor = orgBgColor;
    }


    /// <summary>
    /// Writes a line to the console output with the specified foreground and background colors.
    /// </summary>
    /// <param name="FgColor">Foreground color to use.</param>
    /// <param name="BgColor">Background color to use.</param>
    /// <param name="Format">Composite format string.</param>
    /// <param name="Args">Array of objects to write using format.</param>
    public static void WriteLine(ConsoleColor FgColor, ConsoleColor BgColor, string Format, params object[] Args)
    {
      ConsoleColor orgFgColor = Console.ForegroundColor;
      ConsoleColor orgBgColor = Console.ForegroundColor;
      Console.ForegroundColor = FgColor;
      Console.BackgroundColor = BgColor;
      Console.WriteLine(Format, Args);
      Console.ForegroundColor = orgFgColor;
      Console.BackgroundColor = orgBgColor;
    }

    /// <summary>
    /// Writes to the console output and allows color tags in the text.
    /// </summary>
    /// <param name="Format">Composite format string that may contain color tags.</param>
    /// <param name="Args">Array of objects to write using format.</param>
    public static void WriteRich(string Format, params object[] Args)
    {
      string fullText = string.Format(Format, Args);
      log.Trace("Console: {0}", fullText);

      StringBuilder tagsBuilder = new StringBuilder("");
      StringBuilder regexPatternBuilder = new StringBuilder("");
      foreach (ConsoleColor color in Enum.GetValues(typeof(ConsoleColor)))
      {
        string coloredSubstring = string.Format("<({0})>(.*)</{0}>", color);

        if (regexPatternBuilder.Length > 0) regexPatternBuilder.Append("|");
        regexPatternBuilder.Append(coloredSubstring);

        string tagEnd = string.Format("\\<\\/{0}\\>", color);
        if (tagsBuilder.Length > 0) tagsBuilder.Append("|");
        tagsBuilder.Append(tagEnd);
      }

      string pattern = string.Format("(?<={0})", tagsBuilder.ToString());
      string[] parts = Regex.Split(fullText, pattern, RegexOptions.IgnoreCase);

      foreach (string part in parts)
      {
        pattern = @"(.*)\<(.*)\>(.*)\<\/(\2)\>";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        Match match = regex.Match(part);
        if (match.Success)
        {
          string prefix = match.Groups[1].Value;
          Write(prefix);

          string colorStr = match.Groups[2].Value;
          string coloredString = match.Groups[3].Value;
          ConsoleColor color;
          if (Enum.TryParse(colorStr, true, out color))
          {
            if (SystemInfo.CurrentRuntime.IsLinux() && (color == ConsoleColor.White)) color = ConsoleColor.Cyan;
            Write(color, coloredString);
          }
          else log.Error("Invalid color '{0}'.", colorStr);
        }
        else Write(part);
      }
    }


    /// <summary>
    /// Waits until the user presses one of the allowed keys.
    /// </summary>
    /// <param name="AllowedKeys">List of allowed keys. The default answer has to be the first character in this list.</param>
    /// <returns>Key the user pressed.</returns>
    public static char ReadKeyAnswer(char[] AllowedKeys)
    {
      char res = '§';
      char defaultAnswer = AllowedKeys[0];
      HashSet<char> hs = new HashSet<char>();
      foreach (char c in AllowedKeys)
      {
        hs.Add(Char.ToLowerInvariant(c));
        hs.Add(Char.ToUpperInvariant(c));
      }

      while (!hs.Contains(res))
      {
        ConsoleKeyInfo ki = Console.ReadKey(true);
        res = Char.ToLowerInvariant(ki.KeyChar);

        if (ki.Key == ConsoleKey.Enter) res = defaultAnswer;
      }

      log.Debug("User input: {0}", res);
      WriteRich("<white>{0}</white>\n", res);

      return res;
    }


    /// <summary>
    /// Waits until the user enters a string.
    /// </summary>
    /// <param name="DefaultValue">Default value to be used if user just presses ENTER.</param>
    /// <param name="HideResult">Does not log the actual value the user entered.</param>
    /// <returns>String from the user.</returns>
    public static string ReadStringAnswer(string DefaultValue, bool HideResult = true)
    {
      string res = Console.ReadLine().Trim();

      if (!HideResult) log.Debug("User input: {0}", res);
      else log.Debug("User input: {0}", new string('*', res.Length));

      if (res.Length == 0)
      {
        res = DefaultValue;
        WriteRich("Using '<white>{0}</white>'.\n", res);
      }

      return res;
    }


    /// <summary>
    /// Asks user for a password and then for the confirmation.
    /// </summary>
    /// <param name="FirstMessage">First message to display to the user.</param>
    /// <param name="SecondMessage">Second message to display to the user.</param>
    /// <returns>Password entered by the user.</returns>
    public static string ReadPasswordAnswer(string FirstMessage, string SecondMessage)
    {
      string res = "";
      bool done = false;
      while (!done)
      {
        WriteRich(FirstMessage);
        string pass1 = ReadPasswordInput();
        WriteLine();

        WriteRich(SecondMessage);
        string pass2 = ReadPasswordInput();
        WriteLine();

        if (pass1 == pass2)
        {
          res = pass1;
          break;
        }

        WriteRich("<red>ERROR:</red> Passwords do not match, please try again.\n");
      }

      return res;
    }

    /// <summary>
    /// Reads password from the console input.
    /// </summary>
    /// <returns>Password entered by the user.</returns>
    public static string ReadPasswordInput()
    {
      string res = "";
      ConsoleKeyInfo info = Console.ReadKey(true);
      while (info.Key != ConsoleKey.Enter)
      {
        if (info.Key != ConsoleKey.Backspace)
        {
          Console.Write("*");
          res += info.KeyChar;
        }
        else if (info.Key == ConsoleKey.Backspace)
        {
          if (!string.IsNullOrEmpty(res))
          {
            res = res.Substring(0, res.Length - 1);
            int pos = Console.CursorLeft;
            Console.SetCursorPosition(pos - 1, Console.CursorTop);
            Console.Write(" ");
            Console.SetCursorPosition(pos - 1, Console.CursorTop);
          }
        }

        info = Console.ReadKey(true);
      }
      return res;
    }


    /// <summary>
    /// Writes green OK to the console and ends the line.
    /// </summary>
    public static void WriteOk()
    {
      CUI.WriteRich("<green>OK</green>\n");
    }

    /// <summary>
    /// Writes red FAILED to the console and ends the line.
    /// </summary>
    public static void WriteFailed()
    {
      CUI.WriteRich("<red>FAILED</red>\n");
    }

  }
}
