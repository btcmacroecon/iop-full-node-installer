using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServerInstaller
{
  public class DebPackageInstallationFile: DownloadableInstallationFile
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.DebPackageInstallationFile");

    /// <summary>Name of the package in the package system.</summary>
    protected string packageName;

    /// <summary>
    /// Creates a new instance of the class.
    /// </summary>
    /// <param name="Name">Human readable name of the installation file.</param>
    /// <param name="Uri">URI to download the installation file from.</param>
    /// <param name="PackageName">Name of the package in the package system.</param>
    /// <param name="CanUninstall">If true, the file can be uninstalled.</param>
    public DebPackageInstallationFile(string Name, string Uri, string PackageName, bool CanUninstall = true):
      base(Name, Uri, CanUninstall)
    {
      packageName = PackageName;
    }


    /// <summary>
    /// Installs the file to the system.
    /// </summary>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public override bool Install()
    {
      log.Trace("()");

      bool res = false;

      if (SystemInfo.CurrentRuntime.IsLinux())
      {
        CUI.WriteRich("Downloading <white>{0}</white>... ", Name);
        if (DownloadFileToTemp(uri))
        {
          CUI.WriteOk();

          CUI.WriteRich("Installing .deb package of <white>{0}</white>... ", Name);
          if (InstallDebPackage(downloadedFileName))
          {
            log.Debug(".deb package '{0}' installed.", downloadedFileName);
            Status |= InstallationFileStatus.Installed;
            CUI.WriteOk();
            res = true;
          }
          else
          {
            log.Error("Failed to install .deb package '{0}'.", downloadedFileName);
            CUI.WriteFailed();
          }
        }
        else
        {
          log.Error("Failed to download .deb package from URI '{0}'.", uri);
          CUI.WriteFailed();
        }
      }
      else log.Error(".deb packages are not supported on this system.");

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

      bool error = false;
      if (Status.HasFlag(InstallationFileStatus.Installed))
      {
        if (UninstallDebPackage(packageName))
        {
          Status &= ~InstallationFileStatus.Installed;
        }
        else
        {
          log.Error("Failed to uninstall package '{0}'.", packageName);
          error = true;
        }
      }

      if (!base.Uninstall()) error = true;

      res = !error;

      log.Trace("(-):{0}", res);
      return res;
    }

    /// <summary>
    /// Installs .deb package by executing dpkg.
    /// </summary>
    /// <param name="FileName">.deb package to install.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool InstallDebPackage(string FileName)
    {
      log.Trace("(FileName:'{0}')", FileName);

      bool res = false;

      try
      {
        ConsoleProcess process = new ConsoleProcess("dpkg", string.Format("--install \"{0}\"", FileName));
        if (process.RunAndWaitSuccessExit(120000))
        {
          res = true;
        }
        else log.Error("Executing '{0}' failed.", process.GetCommandLine());
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Uninstalls .deb package by executing dpkg.
    /// </summary>
    /// <param name="PackageName">.deb package to uninstall.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool UninstallDebPackage(string PackageName)
    {
      log.Trace("(PackageName:'{0}')", PackageName);

      bool res = false;

      try
      {
        ConsoleProcess process = new ConsoleProcess("dpkg", string.Format("--remove \"{0}\"", PackageName));
        if (process.RunAndWaitSuccessExit(120000))
        {
          res = true;
        }
        else log.Error("Executing '{0}' failed.", process.GetCommandLine());
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      log.Trace("(-):{0}", res);
      return res;
    }
  }
}
