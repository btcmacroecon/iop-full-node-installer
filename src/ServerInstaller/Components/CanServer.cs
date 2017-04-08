using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServerInstaller
{
  public class CanServer : InstallableComponent
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.CanServer");

    /// <summary>Name of the component.</summary>
    private const string ComponentName = "CAN server";


    /// <summary>List of files that needs to be downloaded mapped by system RIDs.</summary>
    private static Dictionary<Rid, List<InstallationFile>> InstallationFilesByRid = new Dictionary<Rid, List<InstallationFile>>()
    {
      { Rid.win7_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("CAN server", "https://github.com/Fermat-ORG/iop-content-address-network/releases/download/iop-QmQPrdEJpMvD5mdHZh2UAha78cZxcqAg97JundCrgbN6mF/iop-can-windows-amd64-ad911eb.zip", @"%ProgramFiles%\IoP\CAN", true, "CanDir"),
      } },

      { Rid.win81_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("CAN server", "https://github.com/Fermat-ORG/iop-content-address-network/releases/download/iop-QmQPrdEJpMvD5mdHZh2UAha78cZxcqAg97JundCrgbN6mF/iop-can-windows-amd64-ad911eb.zip", @"%ProgramFiles%\IoP\CAN", true, "CanDir"),
      } },

      { Rid.win10_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("CAN server", "https://github.com/Fermat-ORG/iop-content-address-network/releases/download/iop-QmQPrdEJpMvD5mdHZh2UAha78cZxcqAg97JundCrgbN6mF/iop-can-windows-amd64-ad911eb.zip", @"%ProgramFiles%\IoP\CAN", true, "CanDir"),
      } },

      { Rid.ubuntu_14_04_x64, new List<InstallationFile>()
      {
        new TgzArchiveInstallationFile("CAN server", "https://github.com/Fermat-ORG/iop-content-address-network/releases/download/iop-QmQPrdEJpMvD5mdHZh2UAha78cZxcqAg97JundCrgbN6mF/iop-can-linux-amd64-ad911eb.tgz", @"/usr/local/bin/iop-can-server", true, "CanDir"),
      } },

      { Rid.ubuntu_16_04_x64, new List<InstallationFile>()
      {
        new TgzArchiveInstallationFile("CAN server", "https://github.com/Fermat-ORG/iop-content-address-network/releases/download/iop-QmQPrdEJpMvD5mdHZh2UAha78cZxcqAg97JundCrgbN6mF/iop-can-linux-amd64-ad911eb.tgz", @"/usr/local/bin/iop-can-server", true, "CanDir"),
      } },
    };


    /// <summary>List of configuration values mapped by system RIDs.</summary>
    private static Dictionary<Rid, Dictionary<string, string>> ConfigurationByRid = new Dictionary<Rid, Dictionary<string, string>>()
    {
      { Rid.win7_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\.iopcan" },
        { "Configuration file", "config" },
        { "Swarm port", "14001" },
        { "API port", "15001" },
        { "Gateway port", "18080" },
      } },

      { Rid.win81_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\.iopcan" },
        { "Configuration file", "config" },
        { "Swarm port", "14001" },
        { "API port", "15001" },
        { "Gateway port", "18080" },
      } },

      { Rid.win10_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\.iopcan" },
        { "Configuration file", "config" },
        { "Swarm port", "14001" },
        { "API port", "15001" },
        { "Gateway port", "18080" },
      } },

      { Rid.ubuntu_14_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/.iopcan" },
        { "Configuration file", "config" },
        { "Swarm port", "14001" },
        { "API port", "15001" },
        { "Gateway port", "18080" },
      } },

      { Rid.ubuntu_16_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/.iopcan" },
        { "Configuration file", "config" },
        { "Swarm port", "14001" },
        { "API port", "15001" },
        { "Gateway port", "18080" },
      } },
    };


    /// <summary>
    /// Initializes an instance of the component.
    /// </summary>
    public CanServer():
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

      bool done = false;
      while (!done)
      {
        string appDataDir = Environment.ExpandEnvironmentVariables(conf["Application data directory"]);

        string dataDir = InstallationFile.AskForEmptyDirectory(string.Format("Where do you want <white>CAN server application data</white> to be stored? [{0}] ", appDataDir), appDataDir);
        conf["Application data directory"] = dataDir;

        string confFile = Path.Combine(dataDir, conf["Configuration file"]);


        int apiPort = int.Parse(conf["API port"]);
        while (GeneralConfiguration.UsedPorts.ContainsKey(apiPort))
          apiPort++;
        conf["API port"] = apiPort.ToString();
        log.Debug("Selected port {0} as CAN server's API port.", apiPort);
        GeneralConfiguration.UsedPorts.Add(apiPort, "CAN server API interface");
        GeneralConfiguration.SharedValues.Add("CAN server API interface", apiPort.ToString());


        int swarmPort = int.Parse(conf["Swarm port"]);
        swarmPort = AskForOpenPort(string.Format("Please enter a port number for <white>CAN server swarm interface</white>. This port will have to be open and publicly accessible from the Internet. [{0}] ", swarmPort), swarmPort, "CAN server swarm interface");
        conf["Swarm port"] = swarmPort.ToString();

        int gatewayPort = int.Parse(conf["Gateway port"]);
        gatewayPort = AskForOpenPort(string.Format("Please enter a port number for <white>CAN server gateway interface</white>. This port will have to be open and publicly accessible from the Internet. [{0}] ", gatewayPort), gatewayPort, "CAN server gateway interface");
        conf["Gateway port"] = gatewayPort.ToString();


        string ipfs = SystemInfo.CurrentRuntime.IsLinux() ? Path.Combine(GeneralConfiguration.SharedValues["CanDir"], "ipfs") : Path.Combine(GeneralConfiguration.SharedValues["CanDir"], "ipfs.exe");

        string initIpfsScript = SystemInfo.CurrentRuntime.IsLinux() ? Path.Combine(InstallationFile.TemporaryDirectoryName, "ipfsinit.sh") : Path.Combine(InstallationFile.TemporaryDirectoryName, "ipfsinit.cmd");
        string initIpfsScriptContent = SystemInfo.CurrentRuntime.IsLinux() ?
          string.Format("#/bin/base\n"
          + "\n"
          + "export IOPCAN_PATH=\"{0}\"\n"
          + "\"{1}\" init", dataDir, ipfs)
          :
          string.Format("@set \"IOPCAN_PATH={0}\"\n"
          + "\"{1}\" init\n", dataDir, ipfs);


        CUI.WriteRich("Creating <white>CAN server init script</white>... ");
        if (InstallationFile.CreateFileWriteTextChown(initIpfsScript, initIpfsScriptContent))
        {
          CUI.WriteOk();
          CUI.WriteRich("Creating <white>CAN server daemon script</white>... ");

          string daemonIpfsScript = SystemInfo.CurrentRuntime.IsLinux() ? Path.Combine(GeneralConfiguration.SharedValues["CanDir"], "iop-can") : Path.Combine(GeneralConfiguration.SharedValues["CanDir"], "iop-can.cmd");
          string daemonIpfsScriptContent = SystemInfo.CurrentRuntime.IsLinux() ?
            string.Format("#/bin/base\n"
            + "\n"
            + "export IOPCAN_PATH=\"{0}\"\n"
            + "\"{1}\" daemon", dataDir, ipfs)
            :
            string.Format("@set \"IOPCAN_PATH={0}\"\n"
            + "\"{1}\" daemon\n", dataDir, ipfs);

          if (InstallationFile.CreateFileWriteTextChown(daemonIpfsScript, daemonIpfsScriptContent))
          {
            CUI.WriteOk();
            done = true;

            CUI.WriteRich("Starting <white>CAN server init script</white>... ");
            ConsoleProcess initScriptProcess = SystemInfo.CurrentRuntime.IsLinux() ? new ConsoleProcess("bash", string.Format("-x ./{0}", initIpfsScript)) : new ConsoleProcess("cmd.exe", string.Format("/C {0}", initIpfsScript));
            if (initScriptProcess.RunAndWaitSuccessExit(20000))
            {
              CUI.WriteOk();

              InstallationFile.Chown(dataDir);

              CUI.WriteRich("Loading <white>CAN server configuration file</white>... ");
              try
              {
                string[] lines = File.ReadAllLines(confFile);
                CUI.WriteOk();

                bool addressesSection = false;
                List<string> confLines = new List<string>();
                foreach (string line in lines)
                {
                  string confLine = line;

                  if (addressesSection)
                  {
                    if (confLine.Contains("/ip4/0.0.0.0/tcp/14001")) confLine = confLine.Replace("tcp/14001", string.Format("tcp/{0}", conf["Swarm port"]));
                    else if (confLine.Contains("ip6/::/tcp/14001")) confLine = confLine.Replace("tcp/14001", string.Format("tcp/{0}", conf["Swarm port"]));
                    else if (confLine.Contains("/ip4/127.0.0.1/tcp/15001")) confLine = confLine.Replace("tcp/15001", string.Format("tcp/{0}", conf["API port"]));
                    else if (confLine.Contains("/ip4/127.0.0.1/tcp/18080")) confLine = confLine.Replace("tcp/18080", string.Format("tcp/{0}", conf["Gateway port"]));
                    else if (confLine.Trim().EndsWith("},")) addressesSection = false;
                  }
                  else addressesSection = confLine.Contains("\"Addresses\":");

                  confLines.Add(confLine);
                }


                CUI.WriteRich("Writing new <white>CAN server configuration file</white>... ");
                if (InstallationFile.CreateFileWriteTextChown(confFile, string.Join("\n", confLines)))
                {
                  CUI.WriteOk();

                  res = true;
                }
                else
                {
                  CUI.WriteFailed();
                  CUI.WriteRich("<red>ERROR:</red> unable to write to file <white>{0}</white>.\n", confFile);
                }
              }
              catch (Exception e)
              {
                log.Error("Exception occurred: {0}", e.ToString());
                CUI.WriteFailed();
                CUI.WriteRich("<red>ERROR:</red> unable to read file <white>{0}</white>.\n", confFile);
              }
            }
            else CUI.WriteFailed();
          }
          else
          {
            CUI.WriteFailed();
            CUI.WriteRich("<red>ERROR:</red> unable to write to <white>{0}</white>. Please try again.\n", daemonIpfsScript);
          }

          try
          {
            File.Delete(initIpfsScript);
          }
          catch (Exception e)
          {
            log.Error("Exception occurred: {0}", e.ToString());
          }
        }
        else
        {
          CUI.WriteFailed();
          CUI.WriteRich("<red>ERROR:</red> unable to write to <white>{0}</white>. Please try again.\n", initIpfsScript);
        }
      }


      log.Trace("(-):{0}", res);
      return res;
    }

  }
}
