using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ServerInstaller
{
  /// <summary>
  /// Available .NET Core Runtime identifiers as per https://github.com/dotnet/docs/blob/master/docs/core/rid-catalog.md.
  /// </summary>
  public enum Rid
  {
    // Unknown or unsupported runtime.
    Unknown = 0,

    win_min = 1,

    // Windows 7 / Windows Server 2008 R2
    win7_x64 = 1,
    win7_x86 = 2,

    // Windows 8 / Windows Server 2012
    win8_x64 = 10,
    win8_x86 = 11,
    win8_arm = 12,

    // Windows 8.1 / Windows Server 2012 R2
    win81_x64 = 20,
    win81_x86 = 21,
    win81_arm = 22,

    // Windows 10 / Windows Server 2016
    win10_x64 = 31,
    win10_x86 = 32,
    win10_arm = 33,
    win10_arm64 = 34,

    win_max = 99,


    // Linux RIDs

    // Red Hat Enterprise Linux
    linux_min = 100,

    rhel_7_x64 = 100,
    rhel_7_0_x64 = 120,
    rhel_7_1_x64 = 140,
    rhel_7_2_x64 = 160,
    rhel_7_3_x64 = 180,
    rhel_7_4_x64 = 200,

    // Ubuntu
    ubuntu_14_04_x64 = 300,
    ubuntu_14_10_x64 = 320,
    ubuntu_15_04_x64 = 340,
    ubuntu_15_10_x64 = 360,
    ubuntu_16_04_x64 = 380,
    ubuntu_16_10_x64 = 400,

    // CentOS
    centos_7_x64 = 500,

    // Debian
    debian_8_x64 = 600,

    // Fedora
    fedora_23_x64 = 700,
    fedora_24_x64 = 720,

    // OpenSUSE
    opensuse_13_2_x64 = 800,
    opensuse_42_1_x64 = 820,

    // Oracle Linux
    ol_7_x64 = 900,
    ol_7_0_x64 = 920,
    ol_7_1_x64 = 940,
    ol_7_2_x64 = 960,

    // Currently supported Ubuntu derivatives
    linuxmint_17_x64 = 1000,
    linuxmint_17_1_x64 = 1020,
    linuxmint_17_2_x64 = 1040,
    linuxmint_17_3_x64 = 1060,
    linuxmint_18_x64 = 1080,

    linux_max = 1999,

    // OS X RIDs
    osx_min = 2000,

    osx_10_10_x64 = 2000,
    osx_10_11_x64 = 2020,
    osx_10_12_x64 = 2040,

    osx_max = 2999,
  }


  /// <summary>
  /// Information about the current runtime.
  /// </summary>
  public class RuntimeInfo
  {
    /// <summary>Runtime identifier of the current runtime.</summary>
    public Rid Rid;


    /// <summary>
    /// Initializes instance of the class.
    /// </summary>
    public RuntimeInfo():
      this(Rid.Unknown)
    {
    }

    /// <summary>
    /// Initializes instance of the class.
    /// </summary>
    /// <param name="RuntimeId">Runtime identifier of the current runtime.</param>
    public RuntimeInfo(Rid RuntimeId)
    {
      Rid = RuntimeId;
    }

    /// <summary>
    /// Checks whether we are on Linux.
    /// </summary>
    /// <returns>true if the application runs on Linux.</returns>
    public bool IsLinux()
    {
      return (Rid >= Rid.linux_min) && (Rid <= Rid.linux_max);
    }

    /// <summary>
    /// Checks whether we are on Windows.
    /// </summary>
    /// <returns>true if the application runs on Windows.</returns>
    public bool IsWindows()
    {
      return (Rid >= Rid.win_min) && (Rid <= Rid.win_max);
    }

    /// <summary>
    /// Checks whether we are on OS X.
    /// </summary>
    /// <returns>true if the application runs on OS X.</returns>
    public bool IsOSX()
    {
      return (Rid >= Rid.osx_min) && (Rid <= Rid.osx_max);
    }
  }


  /// <summary>
  /// Operating system information routines that allows us to implement platform specific code.
  /// </summary>
  public static class SystemInfo
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.Program");

    /// <summary>Current runtime information.</summary>
    private static RuntimeInfo currentRuntime;
    /// <summary>Current runtime information.</summary>
    public static RuntimeInfo CurrentRuntime
    {
      get
      {
        if (currentRuntime == null) currentRuntime = GetSystemRuntimeInfo();
        return currentRuntime;
      }
    }
  

    /// <summary>
    /// Obtains .NET Core Runtime identifier of the current system.
    /// </summary>
    /// <returns>Runtime identifier of the current system.</returns>
    private static RuntimeInfo GetSystemRuntimeInfo()
    {
      log.Trace("()");

      Rid rid = Rid.Unknown;
      try
      {
        // We only support x64 architecture.
        if (RuntimeInformation.OSArchitecture == Architecture.X64)
        {
          string desc = RuntimeInformation.OSDescription.ToLowerInvariant().Trim();
          log.Debug("OS description '{0}'.", desc);

          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
          {
            log.Debug("Windows platform detected.");

            string winstr = "microsoft windows ";
            if (desc.StartsWith(winstr))
            {
              string ver = desc.Substring(winstr.Length);
              log.Debug("Windows version '{0}' detected.", ver);

              if (ver.StartsWith("6.1.")) rid = Rid.win7_x64;
              else if (ver.StartsWith("6.2.")) rid = Rid.win8_x64;
              else if (ver.StartsWith("6.3.")) rid = Rid.win81_x64;
              else if (ver.StartsWith("10.0.")) rid = Rid.win10_x64;
              else log.Error("Windows version '{0}' is not supported.", ver);
            }
            else log.Error("Invalid or unsupported Windows OS description '{0}'.", desc);
          }
          else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
          {
            log.Debug("Linux platform detected.");

            bool isUbuntu = desc.Contains("ubuntu");
            bool isDebian = desc.Contains("debian");
            if (isUbuntu || isDebian)
            {
              ConsoleProcess lsb = new ConsoleProcess("lsb_release", "-r");
              if (lsb.RunAndWaitSuccessExit())
              {
                List<string> lines = lsb.GetOutput();
                if ((lines != null) && (lines.Count > 0))
                {
                  string ver = lines[0].Trim();
                  int li = ver.LastIndexOf(':');
                  ver = ver.Substring(li + 1).Trim();

                  if (isUbuntu)
                  {
                    if (ver.StartsWith("16.10")) rid = Rid.ubuntu_16_10_x64;
                    else if (ver.StartsWith("16.04")) rid = Rid.ubuntu_16_04_x64;
                    else if (ver.StartsWith("14.10")) rid = Rid.ubuntu_14_10_x64;
                    else if (ver.StartsWith("14.04")) rid = Rid.ubuntu_14_04_x64;
                    else log.Error("Ubuntu version '{0}' is not supported.", ver);
                  }
                  else 
                  {
                    if (ver.StartsWith("8.")) rid = Rid.debian_8_x64;
                    else log.Error("Debian version '{0}' is not supported.", ver);
                  }
                }
                else log.Error("Empty output of 'lsb_release -r' received.");
              }
              else log.Error("Executing '{0}' failed.", lsb.GetCommandLine());
            }
            else log.Error("This Linux distro is not supported.");
          }
          else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
          {
            log.Debug("OSX platform detected.");

            string osxstr = "darwin ";
            if (desc.StartsWith(osxstr))
            {
              string ver = desc.Substring(osxstr.Length);
              log.Debug("OS X version '{0}' detected.", ver);

              if (ver.StartsWith("14.")) rid = Rid.osx_10_10_x64;
              else if (ver.StartsWith("15.")) rid = Rid.osx_10_11_x64;
              else if (ver.StartsWith("16.")) rid = Rid.osx_10_12_x64;
              else log.Error("OS X version '{0}' is not supported.", ver);
            }
            else log.Error("Invalid or unsupported OS X description '{0}'.", desc);
          }
          else log.Error("Unknown OS platform, OS description is '{0}'.", RuntimeInformation.OSDescription);
        }
        else log.Error("OS architecture {0} is not supported.", RuntimeInformation.OSArchitecture);
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());        
      }

      RuntimeInfo res = new RuntimeInfo(rid);

      log.Trace("(-):{0}", res);
      return res;
    }
  }
}
