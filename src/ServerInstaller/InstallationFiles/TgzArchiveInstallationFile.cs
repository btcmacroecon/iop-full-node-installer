using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ServerInstaller
{
  public class TgzArchiveInstallationFile: DownloadableInstallationFile
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.TgzArchiveInstallationFile");

    /// <summary>Name of the directory where the file was unpacked or null if the file was not unpacked.</summary>
    protected string unpackDirectoryName;

    /// <summary>If not null, the final target directory should be saved to GeneralConfiguration.SharedValues under this name.</summary>
    protected string savePathAs;

    /// <summary>
    /// Path within the package (and thus also in the unpacked directory) to important files. 
    /// If not null, the final target directory should be saved to GeneralConfiguration.SharedValues.
    /// </summary>
    protected string internalPath;

    /// <summary>
    /// Creates a new instance of the class.
    /// </summary>
    /// <param name="Name">Human readable name of the installation file.</param>
    /// <param name="Uri">URI to download the installation file from.</param>
    /// <param name="UnpackDirectoryName">Name of the directory where to unpack files. This can contain variables.</param>
    /// <param name="CanUninstall">If true, the file can be uninstalled.</param>
    /// <param name="SavePathAs">If not null, the final target directory should be saved to GeneralConfiguration under this name.</param>
    /// <param name="InternalPath">Path within the package (and thus also in the unpacked directory) to important files.</param>
    public TgzArchiveInstallationFile(string Name, string Uri, string UnpackDirectoryName, bool CanUninstall = true, string SavePathAs = null, string InternalPath = null) :
      base(Name, Uri, CanUninstall)
    {
      unpackDirectoryName = Environment.ExpandEnvironmentVariables(UnpackDirectoryName);
      savePathAs = SavePathAs;
      internalPath = InternalPath;
    }


    /// <summary>
    /// Installs the file to the system.
    /// </summary>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public override bool Install()
    {
      log.Trace("()");

      bool res = false;

      unpackDirectoryName = AskForEmptyDirectory(string.Format("Where do you want to install <white>{0}</white>? [{1}] ", Name, unpackDirectoryName), unpackDirectoryName);

      CUI.WriteRich("Downloading <white>{0}</white>... ", Name);
      if (DownloadFileToTemp(uri))
      {
        CUI.WriteOk();
        CUI.WriteRich("Unpacking <white>{0}</white>... ", Name);
        try
        {
          ConsoleProcess tgz = new ConsoleProcess("tar", string.Format("-xzf \"{0}\" -C \"{1}\" --overwrite", downloadedFileName, unpackDirectoryName));
          if (tgz.RunAndWaitSuccessExit(20000))
          {
            CUI.WriteOk();

            Chown(unpackDirectoryName);

            if (savePathAs != null)
            {
              GeneralConfiguration.SharedValues.Add(savePathAs, unpackDirectoryName);
              if (internalPath != null) GeneralConfiguration.SharedValues.Add(savePathAs + "-InternalPath", Path.Combine(unpackDirectoryName, internalPath));
            }

            Status |= InstallationFileStatus.Unpacked;
            res = true;
          }
          else CUI.WriteFailed();
        }
        catch (Exception e)
        {
          log.Error("Exception occurred: {0}", e.ToString());
          CUI.WriteFailed();
        }
      }
      else
      {
        log.Error("Failed to download file from URI '{0}'.", uri);
        CUI.WriteFailed();
      }

      if (!res) Uninstall();

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Uninstalls the file from the system based on its current status.
    /// </summary>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public override bool Uninstall()
    {
      log.Trace("()");

      bool res = false;

      if (!CanUninstall)
      {
        log.Trace("(-)[NO_UNINSTALL]:{0}", res);
        return res;
      }

      bool error = true;

      if (Status.HasFlag(InstallationFileStatus.Unpacked))
      {
        CUI.WriteRich("Deleting directory of <white>{0}</white>... ", Name);
        if (CleanDirectory(unpackDirectoryName, true))
        {
          Status &= ~InstallationFileStatus.Unpacked;
          CUI.WriteOk();
        }
        else
        {
          log.Error("Failed to delete file '{0}'.", downloadedFileName);
          CUI.WriteFailed();
          error = true;
        }
      }

      if (!base.Uninstall()) error = true;

      res = !error;

      log.Trace("(-):{0}", res);
      return res;
    }
  }
}
