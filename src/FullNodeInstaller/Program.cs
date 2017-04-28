using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace FullNodeInstaller
{
  class Program
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("FullNodeInstaller.Program");

    /// <summary>Version of this installer.</summary>
    private const string Version = "0.2.3";

    /// <summary>Test mode disables some requirements thus allowing testing easily.</summary>
    public static bool TestMode = false;

    /// <summary>On Linux we require the installer to be run under superuser. This is set to "SUDO_UID:SUDO_GID" if the program was run under sudo.</summary>
    public static string SudoUserGroup = null;

    /// <summary>
    /// On Linux we require the installer to be run under superuser. This is set to the value of "SUDO_USER" environmental variable if the program was run under sudo.
    /// On Windows this is set to the value of the "USERNAME" environmental variable.
    /// </summary>
    public static string UserName = null;

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

      // Make sure the current directory is set to the directory of the main executable.
      string path = System.Reflection.Assembly.GetEntryAssembly().Location;
      path = Path.GetDirectoryName(path);
      Directory.SetCurrentDirectory(path);

      InstallationFile.Chown("./Logs");
      log.Debug("Rid is {0}.", SystemInfo.CurrentRuntime.Rid);
      
      if (SystemInfo.CurrentRuntime.IsLinux())
      {
        int sudoUid, sudoGid;
        if (int.TryParse(Environment.ExpandEnvironmentVariables("%SUDO_UID%"), out sudoUid)
          && int.TryParse(Environment.ExpandEnvironmentVariables("%SUDO_GID%"), out sudoGid))
        {
          SudoUserGroup = string.Format("{0}:{1}", sudoUid, sudoGid);
          UserName = Environment.ExpandEnvironmentVariables("%SUDO_USER%");
        }

        if (string.IsNullOrEmpty(UserName)) UserName = "root";
      }
      else if (SystemInfo.CurrentRuntime.IsWindows())
      {
        UserName = Environment.ExpandEnvironmentVariables("%USERNAME%");
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


      if (!error)
      {
        log.Info("Entering autorun installation phase.");

        CUI.WriteRich("\nAll the components are installed and configured now. Would you like to install all components to be started automatically during the startup of your system?"
          + " <yellow>If you answer no, the installer will end and you will have to run the servers manually or setup autoruns yourself.</yellow> [<white>Y</white>ES / <white>n</white>o] ");

        bool installAutorun = CUI.ReadKeyAnswer(new char[] { 'y', 'n' }) == 'y';
        CUI.WriteLine();
        if (installAutorun)
        {
          if (SystemInfo.CurrentRuntime.IsWindows()) WindowsAutorunInitialization();

          // Autorun installation
          foreach (InstallableComponent component in componentList)
          {
            log.Info("Installing autorun for '{0}'.", component.Name);
            CUI.WriteRich("Installing autorun for component <white>{0}</white>.\n", component.Name);

            if (!component.AutorunSetup())
            {
              log.Error("Installing autorun of '{0}' failed.", component.Name);
              CUI.WriteRich("Autorun installation of <white>{0}</white> component <red>FAILED</red>.\n\n", component.Name);
              error = true;
              break;
            }

            CUI.WriteRich("Autorun installation of <white>{0}</white> component <green>SUCCEEDED</green>.\n\n", component.Name);
          }


          if (!error)
          {
            CUI.WriteRich("\nDo you want to start all the installed components now? [<white>Y</white>ES / <white>n</white>o] ");
            bool startAll = CUI.ReadKeyAnswer(new char[] { 'y', 'n' }) == 'y';
            CUI.WriteLine();
            if (startAll)
            {
              foreach (InstallableComponent component in componentList)
              {
                log.Info("Starting '{0}'.", component.Name);
                CUI.WriteRich("Starting component <white>{0}</white>... ", component.Name);

                if (!component.Start())
                {
                  CUI.WriteFailed();
                  log.Error("Starting of '{0}' failed.", component.Name);
                  error = true;
                  break;
                }

                CUI.WriteOk();
              }
            }
          }
        }
        else
        {
          CUI.WriteRich("Autorun installation was skipped. Here are commands with arguments that you can use to run all installed components manually:\n\n");

          // Display paths to executables
          foreach (InstallableComponent component in componentList)
          {
            string exeKey = component.Name + "-executable-args";
            if (GeneralConfiguration.SharedValues.ContainsKey(exeKey))
            {
              string[] parts = GeneralConfiguration.SharedValues[exeKey].Split(new char[] { '\n' });
              foreach (string part in parts)
                CUI.WriteRich(" * {0}: <white>{1}</white>\n", component.Name, part);
            }
          }
        }
      }


      if (error)
      {
        CUI.WriteRich("Installation failed. Would you like to stop and uninstall all components that were installed? [<white>Y</white>ES / <white>n</white>o] ");
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

      if (res)
      {
        CUI.WriteRich("<green>Installation COMPLETE!</green>\n\n");
        SaveInstallerSettings();
      }
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
          "Welcome to <white>IoP Full Node Installer</white>. This installer will download, install and configure all parts of IoP full node:\n"
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
    /// <returns>true if the function succeeded, false otherwise.</returns>
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

    /// <summary>
    /// Asks user about which user and password should be used to run the components.
    /// </summary>
    public static void WindowsAutorunInitialization()
    {
      log.Trace("()");

      CUI.WriteRich("\nEnter name of the user under which the components will be run. <yellow>This user must have a write access to the application data directories you have selected before.</yellow>: [{0}] ", UserName);
      string user = CUI.ReadStringAnswer(UserName);

      string pass = CUI.ReadPasswordAnswer(string.Format("Enter <white>{0}</white>'s password. <yellow>Note that the installer will not check if the password is correct, but if it is not, it will fail.</yellow>: ", user), "Please type the password once again: ");

      GeneralConfiguration.SharedValues.Add("WinTask-User", user);
      GeneralConfiguration.SharedValues.Add("WinTask-Pass", pass);

      CUI.WriteLine();

      log.Trace("(-)");
    }

    /// <summary>
    /// Creates the installer settings file and informs user about it.
    /// </summary>
    public static void SaveInstallerSettings()
    {
      log.Trace("()");

      string home = SystemInfo.CurrentRuntime.IsLinux() ? Environment.ExpandEnvironmentVariables("%HOME%") : Environment.ExpandEnvironmentVariables("%APPDATA%");
      string appDataDir = SystemInfo.CurrentRuntime.IsLinux() ? Path.Combine(home, ".IoP-FullNodeInstaller") : Path.Combine(home, "IoP-FullNodeInstaller");

      StringBuilder sb = new StringBuilder();
      sb.AppendLine(string.Format("installer_version={0}", Version));
      sb.AppendLine();

      sb.AppendLine(string.Format("core_wallet_version=3.0.2", Version));
      sb.AppendLine(string.Format("loc_server_version=1.0.0-a1"));
      sb.AppendLine(string.Format("can_server_version=0.4.5-dev"));
      sb.AppendLine(string.Format("profile_server_version=1.0.2-alpha3"));
      sb.AppendLine();

      string cwDir = GeneralConfiguration.SharedValues.ContainsKey("CoreWalletDir") ? GeneralConfiguration.SharedValues["CoreWalletDir"] : "auto/package";
      sb.AppendLine(string.Format("core_wallet_dir={0}", cwDir));

      string locDir = GeneralConfiguration.SharedValues.ContainsKey("LocDir") ? GeneralConfiguration.SharedValues["LocDir"] : "auto/package";
      sb.AppendLine(string.Format("loc_server_dir={0}", locDir));

      string canDir = GeneralConfiguration.SharedValues["CanDir"];
      sb.AppendLine(string.Format("can_server_dir={0}", canDir));

      string psDir = GeneralConfiguration.SharedValues["PsDir"];
      sb.AppendLine(string.Format("profile_server_dir={0}", psDir));

      string opensslDir = GeneralConfiguration.SharedValues.ContainsKey("OpenSslDir") ? GeneralConfiguration.SharedValues["OpenSslDir"] : "auto/package";
      sb.AppendLine(string.Format("openssl_dir={0}", opensslDir));

      sb.AppendLine();

      string cwDataDir = GeneralConfiguration.SharedValues["CoreWallet-DataDir"];
      sb.AppendLine(string.Format("core_wallet_data_dir={0}", cwDataDir));

      string locDataDir = GeneralConfiguration.SharedValues["Loc-DataDir"];
      sb.AppendLine(string.Format("loc_server_data_dir={0}", locDataDir));

      string canDataDir = GeneralConfiguration.SharedValues["Can-DataDir"];
      sb.AppendLine(string.Format("can_server_data_dir={0}", canDataDir));

      string psDataDir = GeneralConfiguration.SharedValues["Ps-DataDir"];
      sb.AppendLine(string.Format("profile_server_data_dir={0}", psDataDir));

      CUI.WriteRich("Saving Full Node Installer configuration... ");
      string dir = InstallationFile.CreateDirectory(appDataDir);
      if (dir != null)
      {
        string confFile = Path.Combine(dir, "FullNodeInstaller.config");
        if (InstallationFile.CreateFileWriteTextChown(confFile, sb.ToString()))
        {
          CUI.WriteOk();
          CUI.WriteRich("\nPlease note that file <white>{0}</white> has been created and it contains information about the installed components. <yellow>You should preserve this file if you want to upgrade or uninstall the installed components later.</yellow>\n", confFile);
        }
        else
        {
          CUI.WriteFailed();
          CUI.WriteRich("<red>ERROR:</red> Unable to write to file <white>{0}</white>.", confFile);
          log.Error("Unable to write to file '{0}'.", confFile);
        }
      }
      else
      {
        CUI.WriteFailed();
        CUI.WriteRich("<red>ERROR:</red> Unable to create directory <white>{0}</white>.", appDataDir);
        log.Error("Unable to create directory '{0}'.", appDataDir);
      }

      log.Trace("(-)");
    }
  }
}