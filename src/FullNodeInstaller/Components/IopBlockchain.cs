using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FullNodeInstaller
{
  public class IopBlockchain : InstallableComponent
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("FullNodeInstaller.IopBlockchain");

    /// <summary>Name of the component.</summary>
    private const string ComponentName = "Core Wallet";


    /// <summary>List of files that needs to be downloaded mapped by system RIDs.</summary>
    private static Dictionary<Rid, List<InstallationFile>> InstallationFilesByRid = new Dictionary<Rid, List<InstallationFile>>()
    {
      { Rid.win7_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("Core Wallet", "http://repo.fermat.community/windoof/IoP-beta-3.0.2-win64.zip", @"%ProgramFiles%\IoP\Wallet", true, "CoreWalletDir", @"IoP-beta-3.0.2\bin"),
      } },

      { Rid.win81_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("Core Wallet", "http://repo.fermat.community/windoof/IoP-beta-3.0.2-win64.zip", @"%ProgramFiles%\IoP\Wallet", true, "CoreWalletDir", @"IoP-beta-3.0.2\bin"),
      } },

      { Rid.win10_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("Core Wallet", "http://repo.fermat.community/windoof/IoP-beta-3.0.2-win64.zip", @"%ProgramFiles%\IoP\Wallet", true, "CoreWalletDir", @"IoP-beta-3.0.2\bin"),
      } },

      { Rid.ubuntu_14_04_x64, new List<InstallationFile>()
      {
        new AptGetInstallationFile("Core Wallet dependencies", "libprotobuf8 libboost-program-options1.54.0 libboost-thread1.54.0 libboost-chrono1.54.0 libboost-filesystem1.54.0 libqt5gui5 libdb5.3++ libqrencode3 libssl1.0.0 libevent-2.0-5 libevent-pthreads-2.0-5"),
        new DebPackageInstallationFile("Core Wallet", "http://repo.fermat.community/pool/main/i/iop-blockchain/iop-blockchain_3.0.2-ubuntu1404_amd64.deb", "iop-blockchain"),
      } },

      { Rid.ubuntu_16_04_x64, new List<InstallationFile>()
      {
        new AptGetInstallationFile("Core Wallet dependencies", "libprotobuf9v5 libboost-program-options1.58.0 libboost-filesystem1.58.0 libboost-thread1.58.0 libboost-chrono1.58.0 libqt5gui5 libdb5.3++ libqrencode3 libssl1.0.0 libevent-2.0-5 libevent-pthreads-2.0-5"),
        new DebPackageInstallationFile("Core Wallet", "http://repo.fermat.community/pool/main/i/iop-blockchain/iop-blockchain_3.0.2-ubuntu1604_amd64.deb", "iop-blockchain"),
      } },
    };


    /// <summary>List of configuration values mapped by system RIDs.</summary>
    private static Dictionary<Rid, Dictionary<string, string>> ConfigurationByRid = new Dictionary<Rid, Dictionary<string, string>>()
    {
      { Rid.win7_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\IoP" },
        { "Configuration file", @"IoP.conf" },
        { "Main port", "4877" },
        { "Enable RPC", "0" },
        { "RPC user", "IoP" },
        { "RPC port", "8337" },
        { "Enable mining", "0" },
      } },

      { Rid.win81_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\IoP" },
        { "Configuration file", @"IoP.conf" },
        { "Main port", "4877" },
        { "Enable RPC", "0" },
        { "RPC user", "IoP" },
        { "RPC port", "8337" },
        { "Enable mining", "0" },
      } },

      { Rid.win10_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\IoP" },
        { "Configuration file", @"IoP.conf" },
        { "Main port", "4877" },
        { "Enable RPC", "0" },
        { "RPC user", "IoP" },
        { "RPC port", "8337" },
        { "Enable mining", "0" },
      } },

      { Rid.ubuntu_14_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/.IoP" },
        { "Configuration file", @"IoP.conf" },
        { "Main port", "4877" },
        { "Enable RPC", "0" },
        { "RPC user", "IoP" },
        { "RPC port", "8337" },
        { "Enable mining", "0" },
      } },

      { Rid.ubuntu_16_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/.IoP" },
        { "Configuration file", @"IoP.conf" },
        { "Main port", "4877" },
        { "Enable RPC", "0" },
        { "RPC user", "IoP" },
        { "RPC port", "8337" },
        { "Enable mining", "0" },
      } },
    };


    /// <summary>Core wallet configuration file template.</summary>
    private const string ConfigurationFileTemplate =
        "debug=0\n"
      + "testnet=0\n"
      + "listen=1\n"
      + "port=$MAIN_PORT\n"
      + "server=$ENABLE_RPC\n"
      + "rpcuser=$RPC_USER\n"
      + "rpcpassword=$RPC_PASSWORD\n"
      + "rpcport=$RPC_PORT\n"
      + "rpcallowip=0.0.0.0/0\n"
      + "addnode=ham3.fermat.cloud\n"
      + "addnode=ham2.fermat.cloud\n"
      + "addnode=ham4.fermat.cloud\n"
      + "mine=$ENABLE_MINING\n"
      + "minewhitelistaddr=$MINING_LICENSE\n"
      + "minetoaddr=$MINE_TO_ADDR\n";


    /// <summary>Init.d template file name.</summary>
    private const string InitdScriptTemplateFile = "iop-blockchain";

    /// <summary>Name of Windows scheduled task.</summary>
    private const string WinTaskName = "IoP-Blockchain";

    /// <summary>
    /// Initializes an instance of the component.
    /// </summary>
    public IopBlockchain():
      base(ComponentName)
    {
    }

    public override List<InstallationFile> GetInstallationFiles()
    {
      List<InstallationFile> res = null;

      if (!InstallationFilesByRid.TryGetValue(SystemInfo.CurrentRuntime.Rid, out res))
        res = null;

      return res;
    }

    public override bool Configure()
    {
      log.Trace("()");

      bool res = false;

      Dictionary<string, string> conf;
      if (!ConfigurationByRid.TryGetValue(SystemInfo.CurrentRuntime.Rid, out conf))
      {
        log.Trace("(-)[UNSUPPORTED]:{0}", res);
        return res;
      }

      bool rpcPublic = false;
      int rpcPort = int.Parse(conf["RPC port"]);
      string rpcUser = conf["RPC user"];
      string rpcPassword = "xxx";
      string miningLicense = "";
      string miningToAddress = "";
      string miningLicensePrivKey = "";
      bool miningEnabled = false;
      bool rpcEnabled = false;
      string dataDir = "";
      string confFile = "";

      while (!res)
      {
        string appDataDir = Environment.ExpandEnvironmentVariables(conf["Application data directory"]);

        dataDir = InstallationFile.AskForEmptyDirectory(string.Format("Where do you want <white>IoP Blockchain Core wallet application data</white> to be stored? [{0}] ", appDataDir), appDataDir);
        conf["Application data directory"] = dataDir;
        GeneralConfiguration.SharedValues.Add("CoreWallet-DataDir", dataDir);

        confFile = Path.Combine(dataDir, conf["Configuration file"]);

        int mainPort = int.Parse(conf["Main port"]);
        mainPort = AskForOpenPort(string.Format("Please enter a port number for <white>Core wallet P2P interface</white>. This port will have to be open and publicly accessible from the Internet. [{0}] ", mainPort), mainPort, "Core wallet P2P interface");
        conf["Main port"] = mainPort.ToString();

        CUI.WriteRich("Would you like to run <white>Core wallet RPC server</white> (this is required if you want to mine)? [<white>N</white>O / <white>y</white>es] ");
        rpcEnabled = CUI.ReadKeyAnswer(new char[] { 'n', 'y' }) == 'y';
        if (rpcEnabled)
        {
          CUI.WriteRich("Would you like the <white>RPC server</white> to be accessible from the Internet? [<white>N</white>O / <white>y</white>es] ");
          rpcPublic = CUI.ReadKeyAnswer(new char[] { 'n', 'y' }) == 'y';

          if (rpcPublic)
          {
            rpcPort = AskForOpenPort(string.Format("Please enter a port number for <white>Core wallet RPC interface</white>. This port will have to be open and publicly accessible from the Internet. If you changed your mind and you do not want the RPC server to be accessible from the Internet, enter 0. [{0}] ", rpcPort), rpcPort, "Core wallet RPC interface", true);
            if (rpcPort != 0) conf["RPC port"] = rpcPort.ToString();
            else rpcPublic = false;
          }


          CUI.WriteRich("Enter RPC user name: [{0}] ", rpcUser);
          rpcUser = CUI.ReadStringAnswer(rpcUser);
          conf["RPC user"] = rpcUser;

          CUI.WriteRich("Enter RPC password: [{0}] ", rpcPassword);
          rpcPassword = CUI.ReadStringAnswer(rpcPassword, true);

          CUI.WriteRich("Do you have a mining license and would you like to run a miner? [<white>N</white>O / <white>y</white>es] ");
          miningEnabled = CUI.ReadKeyAnswer(new char[] { 'n', 'y' }) == 'y';
          if (miningEnabled)
          {
            CUI.WriteRich("Enter your mining license address (or press ENTER if you changed your mind): ");
            miningLicense = CUI.ReadStringAnswer(miningLicense);

            if (!string.IsNullOrEmpty(miningLicense))
            {
              CUI.WriteRich("Enter the private key for your mining license. <yellow>Note that If you entered a wrong mining license or if you enter wrong private key, this installer will not recognize it, but your Core wallet may fail to start.</yellow> Private key (or press ENTER if you changed your mind): ");
              miningLicensePrivKey = CUI.ReadStringAnswer(miningLicensePrivKey, true);

              if (!string.IsNullOrEmpty(miningLicensePrivKey))
              {
                CUI.WriteRich("Enter the address to which the newly minted coins should be sent (or press ENTER to generate the address automatically): ");
                miningToAddress = CUI.ReadStringAnswer(miningToAddress);
              }
              else
              {
                miningEnabled = false;
                miningLicense = "";
              }
            }
            else miningEnabled = false;
          }
        }

        string confFileContents = ConfigurationFileTemplate
          .Replace("$MAIN_PORT", conf["Main port"])
          .Replace("$ENABLE_RPC", rpcEnabled ? "1" : "0")
          .Replace("$RPC_USER", conf["RPC user"])
          .Replace("$RPC_PASSWORD", rpcPassword)
          .Replace("$RPC_PORT", conf["RPC port"])
          .Replace("$ENABLE_MINING", "0")
          .Replace("$MINING_LICENSE", miningLicense)
          .Replace("$MINE_TO_ADDR", miningToAddress);

        CUI.WriteRich("Writing <white>Core wallet's configuration</white> with mining DISABLED to <white>{0}</white>... ", confFile);
        if (InstallationFile.CreateFileWriteTextChown(confFile, confFileContents))
        {
          CUI.WriteOk();
          res = true;
        }
        else
        {
          CUI.WriteFailed();
          CUI.WriteRich("<red>ERROR:</red> unable to write to <white>{0}</white>. Please try again.\n", confFile);
        }
      }

      string iopd = null;
      if (res)
      {
        iopd = SystemInfo.CurrentRuntime.IsLinux() ? InstallationFile.Which("IoPd") : Path.Combine(GeneralConfiguration.SharedValues["CoreWalletDir-InternalPath"], "IoPd.exe");
        GeneralConfiguration.SharedValues[Name + "-executable"] = iopd;
        GeneralConfiguration.SharedValues[Name + "-executable-args"] = string.Format("\"{0}\" -datadir=\"{1}\"", iopd, GeneralConfiguration.SharedValues["CoreWallet-DataDir"]);
      }

      if (res && miningEnabled)
      {
        log.Trace("Configuring mining.");
        CUI.WriteRich("The basic configuration of <white>Core Wallet</white> is complete, but as you wanted to enable mining, some more configuration is needed. "
          + "Please be patient, it won't take long. If this step fails, the installation will continue and your <white>Core Wallet</white> will be fully setup except for the mining.\n");

        bool miningSetupOk = false;
        bool saveNewConfiguration = false;

        string iopCli = SystemInfo.CurrentRuntime.IsLinux() ? InstallationFile.Which("IoP-cli") : Path.Combine(GeneralConfiguration.SharedValues["CoreWalletDir-InternalPath"], "IoP-cli.exe");

        CUI.WriteRich("Looking for <white>IoPd</white>... ");
        log.Debug("IoPd location is '{0}'.", iopd);
        if (File.Exists(iopd))
        {
          CUI.WriteOk();

          CUI.WriteRich("Looking for <white>IoP-cli</white>... ");
          log.Debug("IoP-cli location is '{0}'.", iopCli);

          if (File.Exists(iopCli))
          {
            CUI.WriteOk();

            CUI.WriteRich("Starting <white>IoPd</white>... ");
            ConsoleProcess iopdProcess = new ConsoleProcess(iopd, string.Format("-datadir=\"{0}\"", dataDir));
            if (iopdProcess.Run())
            {
              Thread.Sleep(5000);
              CUI.WriteOk();

              CUI.WriteRich("Starting <white>IoP-cli</white> to import the private key... ");
              ConsoleProcess iopCliProcess = new ConsoleProcess(iopCli, string.Format("-rpcconnect=127.0.0.1 -rpcport={0} -rpcuser={1} -rpcpassword={2} importprivkey {3}", rpcPort, rpcUser, rpcPassword, miningLicensePrivKey));
              iopCliProcess.LogArguments = string.Format("-rpcconnect=127.0.0.1 -rpcport={0} -rpcuser={1} -rpcpassword={2} importprivkey {3}", rpcPort, rpcUser, new string('*', rpcPassword.Length), new string('*', miningLicensePrivKey.Length));
              if (iopCliProcess.RunAndWaitSuccessExit())
              {
                CUI.WriteOk();

                if (string.IsNullOrEmpty(miningToAddress))
                {
                  CUI.WriteRich("Starting <white>IoP-cli</white> to generate mining-to address... ");
                  iopCliProcess = new ConsoleProcess(iopCli, string.Format("-rpcconnect=127.0.0.1 -rpcport={0} -rpcuser={1} -rpcpassword={2} getnewaddress", rpcPort, rpcUser, rpcPassword));
                  iopCliProcess.LogArguments = string.Format("-rpcconnect=127.0.0.1 -rpcport={0} -rpcuser={1} -rpcpassword={2} getnewaddress", rpcPort, rpcUser, new string('*', rpcPassword.Length));
                  if (iopCliProcess.RunAndWaitSuccessExit())
                  {
                    List<string> outputLines = iopCliProcess.GetOutput();
                    if (outputLines.Count != 0)
                    {
                      miningToAddress = outputLines[0].Trim();
                      CUI.WriteOk();

                      CUI.WriteRich("Your newly generated mining-to address is: <white>{0}</white>\n", miningToAddress);

                      saveNewConfiguration = true;
                    }
                    else
                    {
                      log.Error("Invalid output received from IoP-cli.");
                      CUI.WriteFailed();
                    }
                  }
                  else
                  {
                    log.Error("Unable to start IoP-cli or it failed.");
                    CUI.WriteFailed();
                  }
                }
                else
                {
                  // We do have mining address from user, so we've got everything now.
                  log.Debug("We have mining address from user.");
                  saveNewConfiguration = true;
                }
              }
              else
              {
                log.Error("Unable to start IoP-cli or it failed.");
                CUI.WriteFailed();
              }


              CUI.WriteRich("Starting <white>IoP-cli</white> to stop IoPd... ");
              iopCliProcess = new ConsoleProcess(iopCli, string.Format("-rpcconnect=127.0.0.1 -rpcport={0} -rpcuser={1} -rpcpassword={2} stop", rpcPort, rpcUser, rpcPassword));
              iopCliProcess.LogArguments = string.Format("-rpcconnect=127.0.0.1 -rpcport={0} -rpcuser={1} -rpcpassword={2} stop", rpcPort, rpcUser, new string('*', rpcPassword.Length));
              if (iopCliProcess.RunAndWaitSuccessExit()) CUI.WriteOk();
              else CUI.WriteFailed();

              CUI.WriteRich("Waiting for <white>IoP-cli</white> to stop... ");
              if (iopdProcess.WaitSuccessExit(15000))
              {
                CUI.WriteOk();
              }
              else
              {
                CUI.WriteFailed();
                CUI.WriteLine("IoPd will now be killed.");
                iopdProcess.KillProcess();
              }
            }
            else
            {
              log.Error("Unable to start IoPd.");
              CUI.WriteFailed();
            }
          }
          else
          {
            log.Error("Unable to find IoP-cli.");
            CUI.WriteFailed();
          }
        }
        else
        {
          log.Error("Unable to find IoPd.");
          CUI.WriteFailed();
        }


        if (saveNewConfiguration)
        {
          string confFileContents = ConfigurationFileTemplate
            .Replace("$MAIN_PORT", conf["Main port"])
            .Replace("$ENABLE_RPC", rpcEnabled ? "1" : "0")
            .Replace("$RPC_USER", conf["RPC user"])
            .Replace("$RPC_PASSWORD", rpcPassword)
            .Replace("$RPC_PORT", conf["RPC port"])
            .Replace("$ENABLE_MINING", "1")
            .Replace("$MINING_LICENSE", miningLicense)
            .Replace("$MINE_TO_ADDR", miningToAddress);

          CUI.WriteRich("Writing <white>Core wallet's configuration</white> with mining ENABLED to <white>{0}</white>... ", confFile);
          if (InstallationFile.CreateFileWriteTextChown(confFile, confFileContents))
          {
            CUI.WriteOk();
            miningSetupOk = true;
          }
          else
          {
            CUI.WriteFailed();
            CUI.WriteRich("<red>ERROR:</red> unable to write to <white>{0}</white>.\n", confFile);
          }
        }

        if (miningSetupOk) CUI.WriteRich("Configuration of <white>Core wallet</white> for mining <green>SUCCEEDED</green>.\n");
        else CUI.WriteRich("<red>Configuration of Core wallet for mining failed.</red>\n");
      }

      log.Trace("(-):{0}", res);
      return res;
    }

    public override bool AutorunSetup()
    {
      log.Trace("()");

      bool res = false;

      if (SystemInfo.CurrentRuntime.IsLinux())
      {
        Dictionary<string, string> templateReplacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
          { "{USER}", Program.UserName },
          { "{BIN}", GeneralConfiguration.SharedValues[Name + "-executable"] },
          { "{DATA}", GeneralConfiguration.SharedValues["CoreWallet-DataDir"] },
        };

        res = InstallationFile.InstallInitdScript(InitdScriptTemplateFile, templateReplacements, "start 99 2 3 4 5 . stop 1 0 1 6 .");
      }
      else if (SystemInfo.CurrentRuntime.IsWindows())
      {
        string bin = GeneralConfiguration.SharedValues[Name + "-executable"];
        string args = string.Format("-datadir=\"{0}\"", GeneralConfiguration.SharedValues["CoreWallet-DataDir"]);
        string user = GeneralConfiguration.SharedValues["WinTask-User"];
        string pass = GeneralConfiguration.SharedValues["WinTask-Pass"];
        res = InstallationFile.SchtasksCreate(WinTaskName, bin, args, user, pass);
      }


      if (res)
      {
        Status |= InstallableComponentStatus.AutorunInstalled;
        log.Trace("AutorunInstalled status set for '{0}'.", Name);
      }

      log.Trace("(-):{0}", res);
      return res;
    }

    public override bool AutorunSetupUninstall()
    {
      log.Trace("()");

      bool res = false;

      if (SystemInfo.CurrentRuntime.IsLinux())
      {
        res = InstallationFile.UpdateRcdRemove(InitdScriptTemplateFile);
      }
      else if (SystemInfo.CurrentRuntime.IsWindows())
      {
        res = InstallationFile.SchtasksDelete(WinTaskName);
      }

      if (res) Status &= ~InstallableComponentStatus.AutorunInstalled;

      log.Trace("(-):{0}", res);
      return res;
    }


    public override bool Start()
    {
      log.Trace("()");

      bool res = false;

      if (SystemInfo.CurrentRuntime.IsLinux())
      {
        res = InstallationFile.RunInitdScript(InitdScriptTemplateFile, "start");
      }
      else if (SystemInfo.CurrentRuntime.IsWindows())
      {
        res = InstallationFile.SchtasksRun(WinTaskName);
      }


      if (res)
      {
        Status |= InstallableComponentStatus.Running;
        log.Trace("Running status set for '{0}'.", Name);
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    public override bool Stop()
    {
      log.Trace("()");

      bool res = false;

      if (SystemInfo.CurrentRuntime.IsLinux())
      {
        res = InstallationFile.RunInitdScript(InitdScriptTemplateFile, "stop");
      }
      else if (SystemInfo.CurrentRuntime.IsWindows())
      {
        res = InstallationFile.SchtasksEnd(WinTaskName);
      }


      if (res) Status &= ~InstallableComponentStatus.Running;

      log.Trace("(-):{0}", res);
      return res;
    }

  }
}
