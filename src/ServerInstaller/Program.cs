using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace ServerInstaller
{
  class Program
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.Program");

    /// <summary>Test mode disables some requirements thus allowing testing easily.</summary>
    public static bool TestMode = false;
    
    /// <summary>On Linux we require the installer to be run under superuser. This is set to "SUDO_UID:SUDO_GID" if the program was run under sudo.</summary>
    public static string SudoUserGroup = null;
    
    /// <summary>
    /// Program entry point.
    /// </summary>
    /// <param name="Args">Program command line arguments.</param>
    static void Main(string[] Args)
    {
      log.Trace("(Args:'{0}')", string.Join(" ", Args));

      CUI.InitConsole();

      if ((Args.Length == 1) && (Args[0] == "TestMode"))
      {
        TestMode = true;
        log.Info("Test mode is enabled.");
        CUI.WriteRich("\n<yellow>Test mode ENABLED. Note that in test mode you can skip the port check by using port values between 50001 and 65535.</yellow>\n\n");
      }

      InstallationFile.Chown("./Logs");
      log.Debug("Rid is {0}.", SystemInfo.CurrentRuntime.Rid);
      
      if (SystemInfo.CurrentRuntime.IsLinux())
      {
        int sudoUid, sudoGid;
        if (int.TryParse(Environment.ExpandEnvironmentVariables("%SUDO_UID%"), out sudoUid)
          && int.TryParse(Environment.ExpandEnvironmentVariables("%SUDO_GID%"), out sudoGid))
          SudoUserGroup = string.Format("{0}:{1}", sudoUid, sudoGid);
      }

      switch (SystemInfo.CurrentRuntime.Rid)
      {
        case Rid.ubuntu_14_04_x64:
        case Rid.ubuntu_16_04_x64:
        case Rid.ubuntu_16_10_x64:
        case Rid.win7_x64:
        case Rid.win8_x64:
        case Rid.win81_x64:
        case Rid.win10_x64:
          if (!InstallAll()) log.Error("Installation failed.");
          break;

        default:
          CUI.WriteLine(ConsoleColor.Red, "Error: Your operating system is not supported.");
          break;
      }

      CUI.FinitConsole();
      log.Trace("(-)");
    }


    /// <summary>
    /// Installs all components.
    /// </summary>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public static bool InstallAll()
    {
      log.Trace("()");
      bool res = false;

      if (!Intro())
      {
        log.Trace("(-)[INTRO_FAILED]:{0}", res);
        return res;
      }

      List<InstallableComponent> componentList = new List<InstallableComponent>()
      {
        new IopBlockchain(),
        new LocServer(),
        new CanServer(),
        new ProfileServer(),
      };

      // Download + Installation
      bool error = false;
      foreach (InstallableComponent component in componentList)
      {
        log.Info("Installing '{0}'.", component.Name);
        CUI.WriteRich("Installing component <white>{0}</white>.\n", component.Name);

        if (!component.Install())
        {
          log.Error("Installation of '{0}' failed.", component.Name);
          CUI.WriteRich("Installation of <white>{0}</white> component <red>FAILED</red>.\n\n", component.Name);
          error = true;
          break;
        }

        CUI.WriteRich("Installation of <white>{0}</white> component <green>SUCCEEDED</green>.\n\n", component.Name);
      }


      if (!error)
      {
        log.Info("Entering general configuration phase.");
        if (GeneralConfigurationPhase())
        {
          // Configuration 
          foreach (InstallableComponent component in componentList)
          {
            log.Info("Configuring '{0}'.", component.Name);
            CUI.WriteRich("Configuring component <white>{0}</white>.\n", component.Name);

            if (!component.Configure())
            {
              log.Error("Configuration of '{0}' failed.", component.Name);
              CUI.WriteRich("Configuration of <white>{0}</white> component <red>FAILED</red>.\n\n", component.Name);
              error = true;
              break;
            }

            CUI.WriteRich("Configuration of <white>{0}</white> component <green>SUCCEEDED</green>.\n\n", component.Name);
          }
        }
        else
        {
          CUI.WriteRich("<red>Configuration failed.</red>\n");
          log.Error("General configuration failed.");
          error = true;
        }
      }


      if (error)
      {
        CUI.WriteRich("Installation failed. Would you like to uninstall components that were installed? [<white>Y</white>ES / <white>n</white>o] ");
        bool doUninstall = CUI.ReadKeyAnswer(new char[] { 'y', 'n' }) == 'y';
        if (doUninstall)
        {
          foreach (InstallableComponent component in componentList)
            if (component.Status.HasFlag(InstallableComponentStatus.InstallationCompleted))
            {
              log.Info("Uninstalling '{0}'.", component.Name);
              component.Uninstall();
            }
        }
      }

      res = !error;

      CUI.WriteLine();

      if (res) CUI.WriteRich("<green>Installation COMPLETE!</green>\n");
      else CUI.WriteRich("<red>Installation FAILED!</red>\n");

      CUI.WriteLine();

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Informs user about the program and asks whether they want to continue.
    /// </summary>
    /// <returns>true if the user wants to continue, false otherwise.</returns>
    public static bool Intro()
    {
      log.Trace("()");

      bool res = false;
      CUI.WriteRich(
          "Welcome to <white>IoP Server Installer</white>. This installer will download, install and configure all parts of IoP full node:\n"
        + " <white>* IoP Core Wallet</white>\n"
        + " <white>* IoP LOC Server</white>\n"
        + " <white>* IoP CAN Server</white>\n"
        + " <white>* IoP Profile Server</white>\n"
        + "\n"
        + "It is important to understand that the IoP full node can only operate if you can open several TCP ports in the firewall, "
        + "so that the IoP servers are accessible on your external interface from the Internet.\n"
        + "\n"
        + "<yellow>If this is not the case, you should not continue because you will be unable to complete the installation process.</yellow>\n"
        + "\n"
        + "You will be asked several questions. Some questions will need a full answer from you (e.g. an IP address), "
        + "other questions will ask you to choose one of the options (e.g. yes or no). With full answer questions you may be offered "
        + "a default value, which you can accept just by pressing ENTER. In case of multi choice questions the default value will be written in "
        + " the upper case and if you press ENTER the default value will be selected. To select another option, you will need to press the highlighted "
        + "letter. Let's try it now.\n"
        + "\n"
        + "Do you want to continue? [<white>N</white>O / <white>y</white>es] "
        );

      res = CUI.ReadKeyAnswer(new char[] { 'n', 'y' }) == 'y';

      CUI.WriteLine();

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Informs user that we are going to enter configuration phase and asks several questions regarding general configuration.
    /// </summary>
    /// <returns>true if thefunction succeeded, false otherwise.</returns>
    public static bool GeneralConfigurationPhase()
    {
      log.Trace("()");

      bool res = false;

      CUI.WriteRich("\nWe are going to configure all installed components now.\n");

      CUI.WriteRich("\nTrying to find out what is your external IP address... ");
      IPAddress externalIp = GeneralConfiguration.FindExternalIpAddress();
      string ipDefault = "";
      string externalIpStr = "";
      if (externalIp != null)
      {
        CUI.WriteOk();
        externalIpStr = externalIp.ToString();
        ipDefault = string.Format("[{0}] ", externalIpStr);
      }
      else CUI.WriteFailed();

      bool done = false;
      while (!done)
      {
        CUI.WriteRich("\nEnter external IP address of this machine: {0}", ipDefault);
        string answer = CUI.ReadStringAnswer(externalIpStr);
        IPAddress selectedIp;
        if (IPAddress.TryParse(answer, out selectedIp))
        {
          GeneralConfiguration.ExternalIpAddress = selectedIp;
          done = true;
        }
        else CUI.WriteRich("<red>ERROR:</red> <white>'{0}'</white> is not a valid IP address. Please try again.\n", answer);
      }

      CUI.WriteLine();

      CUI.WriteRich("Trying to find out location of your IP address... ");
      GpsLocation location = GeneralConfiguration.FindIpLocation(GeneralConfiguration.ExternalIpAddress);

      string latitudeStr = "";
      string longitudeStr = "";
      string latitudeDefault = "";
      string longitudeDefault = "";
      if (location != null)
      {
        CUI.WriteOk();
        CultureInfo enUs = new CultureInfo("en-US");
        latitudeStr = location.Latitude.ToString("0.######", enUs);
        longitudeStr = location.Longitude.ToString("0.######", enUs);
        latitudeDefault = string.Format("[{0}] ", latitudeStr);
        longitudeDefault = string.Format("[{0}] ", longitudeStr);
      }
      else CUI.WriteFailed();

      decimal latitude = 0;
      decimal longitude = 0;

      done = false;
      while (!done)
      {
        CUI.WriteRich("\nEnter latitude of GPS location of this machine: {0}", latitudeDefault);
        string answer = CUI.ReadStringAnswer(latitudeStr);

        if (decimal.TryParse(answer, NumberStyles.Any, CultureInfo.InvariantCulture, out latitude) && new GpsLocation(latitude, 0).IsValid()) done = true;
        else CUI.WriteRich("<red>ERROR:</red> <white>'{0}'</white> is not a valid GPS latitude. Please try again.\n", answer);
      }

      done = false;
      while (!done)
      {
        CUI.WriteRich("Enter longitude of GPS location of this machine: {0}", longitudeDefault);
        string answer = CUI.ReadStringAnswer(longitudeStr);

        if (decimal.TryParse(answer, NumberStyles.Any, CultureInfo.InvariantCulture, out longitude) && new GpsLocation(latitude, longitude).IsValid()) done = true;
        else CUI.WriteRich("<red>ERROR:</red> <white>'{0}'</white> is not a valid GPS longitude. Please try again.\n", answer);
      }

      GeneralConfiguration.Location = new GpsLocation(latitude, longitude);

      CUI.WriteLine();
      res = true;


      log.Trace("(-):{0}", res);
      return res;
    }
  }
}