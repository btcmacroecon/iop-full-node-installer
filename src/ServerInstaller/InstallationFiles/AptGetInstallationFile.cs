using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerInstaller
{
  public class AptGetInstallationFile : InstallationFile
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.AptGetInstallationFile");

    /// <summary>Name of the package in the package system.</summary>
    protected string packageName;


    /// <summary>
    /// Creates a new instance of the class.
    /// </summary>
    /// <param name="Name">Human readable name of the installation file.</param>
    /// <param name="PackageName">Name of the package in the package system.</param>
    /// <param name="CanUninstall">If true, the file can be uninstalled.</param>
    public AptGetInstallationFile(string Name, string PackageName, bool CanUninstall = false):
      base(Name, CanUninstall)
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
        CUI.WriteRich("Installing package <white>{0}</white>... ", packageName);
        if (InstallAptGetPackage(packageName))
        {
          log.Debug("apt-get package '{0}' installed.", packageName);
          Status |= InstallationFileStatus.Installed;
          CUI.WriteOk();
          res = true;
        }
        else
        {
          log.Error("Failed to install apt-get package '{0}'.", packageName);
          CUI.WriteFailed();
        }

        if (!res) Uninstall();
      }
      else log.Error(".deb packages are not supported on this system.");

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
        CUI.WriteRich("Removing package <white>{0}</white>... ", packageName);
        if (UninstallAptGetPackage(packageName))
        {
          Status &= ~InstallationFileStatus.Installed;
          CUI.WriteOk();
        }
        else
        {
          log.Error("Failed to uninstall package '{0}'.", packageName);
          CUI.WriteFailed();
          error = true;
        }
      }

      res = !error;

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Installs package using apt-get command.
    /// </summary>
    /// <param name="PackageName">Package package to install.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool InstallAptGetPackage(string PackageName)
    {
      log.Trace("(PackageName:'{0}')", PackageName);

      bool res = false;

      try
      {
        ConsoleProcess process = new ConsoleProcess("apt-get", string.Format("install -qy {0}", PackageName));
        if (process.RunAndWaitSuccessExit(180000)) 
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
    /// Uninstalls package by executing apt-get remove.
    /// </summary>
    /// <param name="PackageName">Package to uninstall.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool UninstallAptGetPackage(string PackageName)
    {
      log.Trace("(FileName:'{0}')", PackageName);

      bool res = false;

      try
      {
        ConsoleProcess process = new ConsoleProcess("apt-get", string.Format("remove -qy {0}", PackageName));
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
