using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace ServerInstaller
{
  /// <summary>Possible states of the installation file.</summary>
  [Flags]
  public enum InstallationFileStatus
  {
    /// <summary>The file was not touched yet.</summary>
    None = 0,

    /// <summary>The file was downloaded to the local disk.</summary>
    Downloaded = 1,

    /// <summary>The file was unpacked to the folder on the local disk.</summary>
    Unpacked = 2,

    /// <summary>The file was installed.</summary>
    Installed = 4
  }

  /// <summary>
  /// Representation of an installation file of a single server or component.
  /// </summary>
  public abstract class InstallationFile
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.InstallationFile");

    /// <summary>
    /// Name of the temporary directory.
    /// </summary>
    public const string TemporaryDirectoryName = "tmp";

    /// <summary>Human readable name of the installation file.</summary>
    public string Name;

    /// <summary>Status of the component.</summary>
    public InstallationFileStatus Status = InstallationFileStatus.None;

    /// <summary>Ture if the file can be uninstalled, false otherwise.</summary>
    public bool CanUninstall;


    /// <summary>
    /// Installs the file to the system.
    /// </summary>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public abstract bool Install();

    /// <summary>
    /// Uninstalls the file from the system based on its current status.
    /// </summary>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public abstract bool Uninstall();


    /// <summary>
    /// Initializes temporary directory within the program location.
    /// </summary>
    static InstallationFile()
    {
      log.Trace("()");

      if (Directory.Exists(TemporaryDirectoryName)) CleanDirectory(TemporaryDirectoryName);
      else CreateDirectory(TemporaryDirectoryName);

      log.Trace("(-)");
    }

    /// <summary>
    /// Creates a new instance of the class.
    /// </summary>
    /// <param name="Name">Human readable name of the installation file.</param>
    /// <param name="CanUninstall">If true, the file can be uninstalled.</param>
    public InstallationFile(string Name, bool CanUninstall)
    {
      this.Name = Name;
      this.CanUninstall = CanUninstall;
    }



    /// <summary>
    /// Generates a name for a temporary file in the temporary directory.
    /// </summary>
    /// <param name="Uri">URI of the file.</param>
    /// <returns>Name of the file including path.</returns>
    public string GenerateTemporaryFileName(Uri Uri)
    {
      log.Trace("()");

      string paq = Uri.AbsolutePath;
      string fileName = paq.Substring(paq.LastIndexOf('/') + 1);
      string res = Path.Combine(TemporaryDirectoryName, fileName);

      log.Trace("(-):{0}", res);
      return res;
    }



    /// <summary>
    /// Removes all files and folders from a specific directory.
    /// </summary>
    /// <param name="DirectoryName">Name of the directory to clean.</param>
    /// <param name="DeleteDirectoryItself">If set to true, the directory itself will be deleted.</param>
    /// <returns>true if the function succeeds, false otherwise</returns>
    public static bool CleanDirectory(string DirectoryName, bool DeleteDirectoryItself = false)
    {
      log.Trace("(DirectoryName:'{0}')", DirectoryName);

      bool res = false;
      try
      {
        string existingDirectoryName;
        if (FindDirectory(DirectoryName, out existingDirectoryName))
        {
          DirectoryInfo di = new DirectoryInfo(DirectoryName);

          foreach (FileInfo file in di.GetFiles())
            file.Delete();

          foreach (DirectoryInfo dir in di.GetDirectories())
            dir.Delete(true);

          if (DeleteDirectoryItself) Directory.Delete(existingDirectoryName);
          res = true;
        }
        else log.Error("Directory '{0}' not found.", DirectoryName);
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      log.Trace("(-):{0}", res);
      return res;
    }



    /// <summary>
    /// Tries to find a file using its name or path.
    /// </summary>
    /// <param name="FileName">Name of the file or relative or full path to the file.</param>
    /// <param name="ExistingFileName">String to receive the name of an existing file if the function succeeds.</param>
    /// <returns>true if the file is found, false otherwise.</returns>
    public static bool FindFile(string FileName, out string ExistingFileName)
    {
      log.Trace("(FileName:'{0}')", FileName);

      bool res = false;
      ExistingFileName = null;
      if (File.Exists(FileName))
      {
        ExistingFileName = FileName;
        res = true;
      }
      else
      {
        string path = System.Reflection.Assembly.GetEntryAssembly().Location;
        path = Path.GetDirectoryName(path);
        path = Path.Combine(path, FileName);
        log.Trace("Checking path '{0}'.", path);
        if (File.Exists(path))
        {
          ExistingFileName = path;
          res = true;
        }
      }

      if (res) log.Trace("(-):{0},ExistingFileName='{1}'", res, ExistingFileName);
      else log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Converts a file name path to a full path.
    /// </summary>
    /// <param name="FileName">Name of the file or relative or full path to the file.</param>
    /// <param name="FullFileName">String to receive the name of an existing file including the full path if the function succeeds.</param>
    /// <returns>true if the full file name was found, false otherwise.</returns>
    public static bool GetFullFileName(string FileName, out string FullFileName)
    {
      log.Trace("(FileName:'{0}')", FileName);

      bool res = false;
      FullFileName = null;

      try
      {
        string path = System.Reflection.Assembly.GetEntryAssembly().Location;
        path = Path.GetDirectoryName(path);
        path = Path.Combine(path, FileName);
        log.Trace("Checking path '{0}'.", path);
        if (File.Exists(path))
        {
          FullFileName = path;
          res = true;
        }
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      if (res) log.Trace("(-):{0},FullFileName='{1}'", res, FullFileName);
      else log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Tries to find a directory using its name or path.
    /// </summary>
    /// <param name="DirectoryName">Name of the directory or relative or full path to the directory.</param>
    /// <param name="ExistingDirectoryName">String to receive the name of an existing directory if the function succeeds.</param>
    /// <returns>true if the directory is found, false otherwise.</returns>
    public static bool FindDirectory(string DirectoryName, out string ExistingDirectoryName)
    {
      log.Trace("(DirectoryName:'{0}')", DirectoryName);

      bool res = false;
      ExistingDirectoryName = null;
      if (Directory.Exists(DirectoryName))
      {
        ExistingDirectoryName = DirectoryName;
        res = true;
      }
      else
      {
        try
        {
          string path = System.Reflection.Assembly.GetEntryAssembly().Location;
          path = Path.GetDirectoryName(path);
          path = Path.Combine(path, DirectoryName);
          log.Trace("Checking path '{0}'.", path);
          if (Directory.Exists(path))
          {
            ExistingDirectoryName = path;
            res = true;
          }
        }
        catch (Exception e)
        {
          log.Error("Exception occurred: {0}", e.ToString());
        }
      }

      if (res) log.Trace("(-):{0},ExistingDirectoryName='{1}'", res, ExistingDirectoryName);
      else log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Safely delete file.
    /// </summary>
    /// <param name="FileName">File to delete.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool DeleteFile(string FileName)
    {
      log.Trace("(FileName:'{0}')", FileName);

      bool res = false;

      try
      {
        if (File.Exists(FileName))
        {
          File.Delete(FileName);
          res = true;
        }
        else log.Error("File '{0}' does not exist.", FileName);
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Checks if a directory is empty.
    /// </summary>
    /// <param name="DirectoryName">Name of the directory to check.</param>
    /// <returns>true if the directory is empty, false otherwise or if the function fails.</returns>
    public static bool IsEmptyDirectory(string DirectoryName)
    {
      log.Trace("(DirectoryName:'{0}')", DirectoryName);

      bool res = false;

      string directoryName;
      if (FindDirectory(DirectoryName, out directoryName))
      {
        try
        {
          DirectoryInfo di = new DirectoryInfo(DirectoryName);

          res = (di.GetFiles().Length == 0) && (di.GetDirectories().Length == 0);
        }
        catch (Exception e)
        {
          log.Error("Exception occurred: {0}", e.ToString());
        }
      }
      else log.Error("Directory '{0}' not found.", DirectoryName);

      log.Trace("(-):{0}", res);
      return res;
    }



    /// <summary>
    /// Creates a new directory.
    /// </summary>
    /// <param name="DirectoryName">Name of the directory to check.</param>
    /// <param name="ChangeOwner">If set to true and we are running on Linux under sudo, we use chown to change owner of the new directory.</param>
    /// <returns>Full name of the directory or null if the function fails.</returns>
    public static string CreateDirectory(string DirectoryName)
    {
      log.Trace("(DirectoryName:'{0}')", DirectoryName);

      string res = null;
      try
      {
        Directory.CreateDirectory(DirectoryName);
        FindDirectory(DirectoryName, out res);

        if (res != null) Chown(res);
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      log.Trace("(-):'{0}'", res);
      return res;
    }

    /// <summary>
    /// Change owner of file or folder on Linux using chown.
    /// </summary>
    /// <param name="Path">Path to file or folder.</param>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public static bool Chown(string Path)
    {
      log.Trace("(Path:'{0}')", Path);
      bool res = false;

      if (!SystemInfo.CurrentRuntime.IsLinux())
      {
        res = true;
        log.Trace("(-)[NOT_LINUX]:{0}", res);
        return res;
      }

      if (string.IsNullOrEmpty(Program.SudoUserGroup))
      {
        res = true;
        log.Trace("(-)[NOT_SUDO]:{0}", res);
        return res;
      }

      ConsoleProcess chown = new ConsoleProcess("chown", string.Format("-R {0} \"{1}\"", Program.SudoUserGroup, Path));
      log.Debug("Starting '{0}'.", chown.GetCommandLine());
      if (chown.RunAndWaitSuccessExit())
      {
        log.Debug("Owner of '{0}' changed.", Path);
        res = true;
      }
      else log.Error("chown on '{0}' failed.", Path);

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Change access rights of file on Linux using chmod.
    /// </summary>
    /// <param name="Path">Path to file or folder.</param>
    /// <param name="AccessRights">chmod access rights.</param>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public static bool Chmod(string Path, string AccessRights)
    {
      log.Trace("(Path:'{0}',AccessRights:'{1}')", Path, AccessRights);
      bool res = false;

      if (!SystemInfo.CurrentRuntime.IsLinux())
      {
        res = true;
        log.Trace("(-)[NOT_LINUX]:{0}", res);
        return res;
      }

      ConsoleProcess chmod = new ConsoleProcess("chmod", string.Format("{0} \"{1}\"", AccessRights, Path));
      log.Debug("Starting '{0}'.", chmod.GetCommandLine());
      if (chmod.RunAndWaitSuccessExit())
      {
        log.Debug("Access rights of '{0}' changed.", Path);
        res = true;
      }
      else log.Error("chmod on '{0}' failed.", Path);

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Installs init script links on Linux using update-rc.d.
    /// </summary>
    /// <param name="ScriptName">Name of the init.d script to install.</param>
    /// <param name="StartupArguments">chmod access rights.</param>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public static bool UpdateRcdInstall(string ScriptName, string StartupArguments)
    {
      return UpdateRcd(string.Format("{0} {1}", ScriptName, StartupArguments));
    }

    /// <summary>
    /// Uninstalls init script links on Linux using update-rc.d.
    /// </summary>
    /// <param name="ScriptName">Name of the init.d script to install.</param>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public static bool UpdateRcdRemove(string ScriptName)
    {
      return UpdateRcd(string.Format("-f {0} remove", ScriptName));
    }


    /// <summary>
    /// Executes update-rc.d on Linux.
    /// </summary>
    /// <param name="Args">Arguments for the command.</param>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public static bool UpdateRcd(string Args)
    {
      log.Trace("(Args:'{0}')", Args);
      bool res = false;

      if (!SystemInfo.CurrentRuntime.IsLinux())
      {
        res = true;
        log.Trace("(-)[NOT_LINUX]:{0}", res);
        return res;
      }

      ConsoleProcess updateRcd = new ConsoleProcess("update-rc.d", Args);
      log.Debug("Starting '{0}'.", updateRcd.GetCommandLine());
      if (updateRcd.RunAndWaitSuccessExit())
      {
        log.Debug("update-rc.d succeeded.");
        res = true;
      }
      else log.Error("update-rc.d failed.");

      log.Trace("(-):{0}", res);
      return res;
    }



    /// <summary>
    /// Locates file on Linux using which.
    /// </summary>
    /// <param name="FileName">Name of file to look for.</param>
    /// <returns>Result of which or null if the function failed.</returns>
    public static string Which(string FileName)
    {
      log.Trace("(FileName:'{0}')", FileName);
      string res = null;

      if (!SystemInfo.CurrentRuntime.IsLinux())
      {
        log.Trace("(-)[NOT_LINUX]:'{0}'", res);
        return res;
      }

      ConsoleProcess which = new ConsoleProcess("which", FileName);
      log.Debug("Starting '{0}'.", which.GetCommandLine());
      if (which.RunAndWaitSuccessExit())
      {
        List<string> outputLines = which.GetOutput();
        if (outputLines.Count > 0) res = outputLines[0].Trim();
        if (string.IsNullOrEmpty(res)) res = null;
      }
      else log.Error("Which on '{0}' failed.", FileName);

      log.Trace("(-):'{0}'", res);
      return res;
    }


    /// <summary>
    /// Asks user to input a name of an empty or non-existing directory.
    /// </summary>
    /// <param name="RichMessage">Message to print on the console.</param>
    /// <param name="DefaultValue">Default value for the directory name.</param>
    /// <returns>Selected directory name that is empty.</returns>
    public static string AskForEmptyDirectory(string RichMessage, string DefaultValue)
    {
      log.Trace("(DefaultValue:'{0}')", DefaultValue);

      string res = "";

      bool done = false;
      while (!done)
      {
        CUI.WriteRich(RichMessage);
        string answer = CUI.ReadStringAnswer(DefaultValue);

        string existingDir;
        if (FindDirectory(answer, out existingDir))
        {
          if (IsEmptyDirectory(existingDir))
          {
            res = existingDir;
            done = true;
          }
          else
          {
            CUI.WriteRich("\n<yellow>Directory '{0}' is not empty.</yellow> Do you want to erase its contents or select another directory? [<white>S</white>ELECT ANOTHER / <white>e</white>rase contents] ", existingDir);
            bool eraseDirectory = CUI.ReadKeyAnswer(new char[] { 's', 'e' }) == 'e';
            if (eraseDirectory)
            {
              CUI.WriteRich("Erasing directory <white>'{0}'</white>... ", existingDir);
              if (CleanDirectory(existingDir))
              {
                CUI.WriteOk();
                res = existingDir;
                done = true;
              }
              else
              {
                CUI.WriteFailed();
                CUI.WriteRich("\n<red>ERROR:</red> Unable to erase contents of directory <white>'{0}'</white>. Please select different directory.\n", existingDir);
              }
            }
          }
        }
        else
        {
          existingDir = CreateDirectory(answer);
          if (existingDir != null)
          {
            res = existingDir;
            done = true;
          }
          else CUI.WriteRich("\n<red>ERROR:</red> Unable to create directory <white>'{0}'</white>. Please try again.\n", answer);
        }
      }

      CUI.WriteLine();

      log.Trace("(-):'{0}'", res);
      return res;
    }

    /// <summary>
    /// Writes text to file and on Linux it chowns it.
    /// </summary>
    /// <param name="FileName">Name of the file.</param>
    /// <param name="Contents">File contents.</param>
    /// <returns>true if the function succeeds (result of chown command is ignored), false otherwise.</returns>
    public static bool CreateFileWriteTextChown(string FileName, string Contents)
    {
      log.Trace("(FileName:'{0}')", FileName);

      bool res = false;

      try
      {
        File.WriteAllText(FileName, Contents);
        res = true;
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      if (res) Chown(FileName);

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Generates PFX certificate using OpenSSL.
    /// </summary>
    /// <param name="OpenSsl">Path to OpenSSL.</param>
    /// <param name="PfxFile">Path where to store the final PFX file.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool GeneratePfxCertificate(string OpenSsl, string PfxFile)
    {
      log.Trace("(OpenSsl:'{0}',PfxFile:'{1}')", OpenSsl, PfxFile);

      bool res = false;

      string opensslScriptContents;
      if (SystemInfo.CurrentRuntime.IsWindows())
      {
        // On Windows we have to create script to run OpenSSL with environmental variable pointing to the OpenSSL configuration file.
        string path = System.Reflection.Assembly.GetEntryAssembly().Location;
        path = Path.GetDirectoryName(path);
        path = Path.Combine(path, "openssl.cfg");

        opensslScriptContents =
          string.Format(
            "@set \"OPENSSL_CONF={0}\"\n"
          + "@\"{1}\" req -x509 -newkey rsa:4096 -keyout cert.key -out cert.cer -days 365000 -subj \"/C=FM\" -passout pass:1234\n"
          + "@\"{1}\" pkcs12 -export -out \"{2}\" -inkey cert.key -in cert.cer -passin pass:1234 -passout \"pass:\"\n"
          + "@del cert.key\n"
          + "@del cert.cer\n",
            path, OpenSsl, PfxFile);
      }
      else
      {
        opensslScriptContents =
          string.Format(
            "\"{0}\" req -x509 -newkey rsa:4096 -keyout cert.key -out cert.cer -days 365000 -subj \"/C=FM\" -passout pass:1234\n"
          + "\"{0}\" pkcs12 -export -out \"{1}\" -inkey cert.key -in cert.cer -passin pass:1234 -passout \"pass:\"\n"
          + "rm cert.key\n"
          + "rm cert.cer\n",
            OpenSsl, PfxFile);
      }

      string opensslScript = Path.Combine(TemporaryDirectoryName, SystemInfo.CurrentRuntime.IsLinux() ? "gencert.sh" : "gencert.cmd");
      if (CreateFileWriteTextChown(opensslScript, opensslScriptContents))
      {
        ConsoleProcess scriptProcess = SystemInfo.CurrentRuntime.IsLinux() ? new ConsoleProcess("bash", string.Format("-x ./{0}", opensslScript)) : new ConsoleProcess("cmd.exe", string.Format("/C {0}", opensslScript));
        if (scriptProcess.RunAndWaitSuccessExit(20000))
        {
          Chown(PfxFile);
          res = File.Exists(PfxFile);
        }
        else log.Error("Certificate generation script failed.");
      }
      else log.Error("Unable to write certificate generation script to '{0}'.", opensslScript);

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Installs init.d script on Linux from script template file.
    /// </summary>
    /// <param name="TemplateFile">Name of the template file, which is also a name of the init.d script.</param>
    /// <param name="Replacements">List of template replacements mapped by the patterns to replace.</param>
    /// <param name="UpdateRcdInstallArgs">Arguments for update-rc.d command for script installation.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool InstallInitdScript(string TemplateFile, Dictionary<string, string> Replacements, string UpdateRcdInstallArgs)
    {
      log.Trace("(TemplateFile:'{0}',UpdateRcdInstallArgs:'{1}')", TemplateFile, UpdateRcdInstallArgs);

      bool res = false;

      CUI.WriteRich("Creating init.d script <white>{0}</white>... ", TemplateFile);

      bool error = true;
      string content = null;
      string initdScript = Path.Combine("init.d", TemplateFile);
      string destFile = string.Format("/etc/init.d/{0}", TemplateFile);
      try
      {
        content = File.ReadAllText(initdScript);
        foreach (KeyValuePair<string, string> kvp in Replacements)
          content = content.Replace(kvp.Key, kvp.Value);

        File.WriteAllText(destFile, content);

        if (Chmod(destFile, "a+x"))
        {
          CUI.WriteOk();
          error = false;
        }
        else
        {
          log.Error("chmod on '{0}' failed.", destFile);
          CUI.WriteFailed();
          CUI.Write("<red>ERROR:</red> Unable to set access rights of file <white>{0}</white>.\n", initdScript);
        }
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
        CUI.WriteFailed();
        if (content == null) CUI.Write("<red>ERROR:</red> Unable to read file <white>{0}</white>: {1}\n", initdScript, e.Message);
        else CUI.Write("<red>ERROR:</red> Unable to write to file <white>{0}</white>: {1}\n", destFile, e.Message);
      }

      if (!error)
      {
        CUI.WriteRich("Installing init script links for <white>{0}</white>... ", TemplateFile);
        if (UpdateRcdInstall(TemplateFile, UpdateRcdInstallArgs))
        {
          CUI.WriteOk();
          res = true;
        }
        else
        {
          log.Error("update-rc.d failed for '{0}'.", TemplateFile);
          CUI.WriteFailed();
        }
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Starts init.d script on Linux.
    /// </summary>
    /// <param name="ScriptName">Name of the init.d script to start.</param>
    /// <param name="Args">Arguments for the command.</param>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public static bool RunInitdScript(string ScriptName, string Args)
    {
      log.Trace("(ScriptName:'{0}',Args:'{1}')", ScriptName, Args);
      bool res = false;

      if (!SystemInfo.CurrentRuntime.IsLinux())
      {
        res = true;
        log.Trace("(-)[NOT_LINUX]:{0}", res);
        return res;
      }

      string scriptFile = string.Format("/etc/init.d/{0}", ScriptName);
      ConsoleProcess script = new ConsoleProcess(scriptFile, Args);
      log.Debug("Starting '{0}'.", script.GetCommandLine());
      if (script.RunAndWaitSuccessExit())
      {
        log.Debug("'{0}' succeeded.", ScriptName);
        res = true;
      }
      else log.Error("'{0}' failed.", ScriptName);

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Creates a new scheduled task to run after the system starts using schtasks command on Windows.
    /// </summary>
    /// <param name="TaskName">Name of the task to create.</param>
    /// <param name="Program">Program to execute.</param>
    /// <param name="Arguments">Arguments for the program to start with.</param>
    /// <param name="User">Name of the user to start the program under.</param>
    /// <param name="Password">User's password.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool SchtasksCreate(string TaskName, string Program, string Arguments, string User, string Password)
    {
      log.Trace("(TaskName:'{0}',Program:'{1}',Arguments:'{2}',User:'{3}',Password:'{4}')", TaskName, Program, Arguments, User, Password);

      bool res = false;

      try
      {
        string taskProgramPath = Path.GetDirectoryName(Program);
        string taskTemplate = File.ReadAllText(Path.Combine("init.d", "win-task-template.xml"));
        string taskXmlContents = taskTemplate
          .Replace("{USER}", User)
          .Replace("{BIN}", Program)
          .Replace("{ARGS}", Arguments)
          .Replace("{PATH}", taskProgramPath);

        string taskXml = Path.Combine(TemporaryDirectoryName, string.Format("{0}.xml", TaskName));
        File.WriteAllText(taskXml, taskXmlContents);

        res = Schtasks(string.Format("/create /xml \"{0}\" /tn {1} /ru {2} /rp \"{3}\"", taskXml, TaskName, User, Password));
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Runs a scheduled task on Windows using schtasks.
    /// </summary>
    /// <param name="TaskName">Name of the task to run.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool SchtasksRun(string TaskName)
    {
      return Schtasks(string.Format("/run /tn {0}", TaskName));
    }


    /// <summary>
    /// Terminates a scheduled task on Windows using schtasks.
    /// </summary>
    /// <param name="TaskName">Name of the task to terminate.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool SchtasksEnd(string TaskName)
    {
      return Schtasks(string.Format("/end /tn {0}", TaskName));
    }


    /// <summary>
    /// Deletes a scheduled task on Windows using schtasks.
    /// </summary>
    /// <param name="TaskName">Name of the task to delete.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool SchtasksDelete(string TaskName)
    {
      return Schtasks(string.Format("/delete /f /tn {0}", TaskName));
    }


    /// <summary>
    /// Executes schtasks command on Windows.
    /// </summary>
    /// <param name="Arguments">Arguments for schtasks command.</param>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public static bool Schtasks(string Arguments)
    {
      log.Trace("(Arguments:'{0}')", Arguments);
      bool res = false;

      if (!SystemInfo.CurrentRuntime.IsWindows())
      {
        res = true;
        log.Trace("(-)[NOT_WINDOWS]:{0}", res);
        return res;
      }

      ConsoleProcess schtasks = new ConsoleProcess("schtasks.exe", Arguments);
      log.Debug("Starting '{0}'.", schtasks.GetCommandLine());
      if (schtasks.RunAndWaitSuccessExit())
      {
        log.Debug("schtasks succeeded.");
        res = true;
      }
      else log.Error("schtasks failed.");

      log.Trace("(-):{0}", res);
      return res;
    }

  }
}
