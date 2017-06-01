using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace FullNodeInstaller
{
  public class LocServer : InstallableComponent
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("FullNodeInstaller.LocServer");

    /// <summary>Name of the component.</summary>
    private const string ComponentName = "LOC server";

    /// <summary>Version of this component.</summary>
    public const string Version = "1.0.0-alpha1";


    /// <summary>List of files that needs to be downloaded mapped by system RIDs.</summary>
    private static Dictionary<Rid, List<InstallationFile>> InstallationFilesByRid = new Dictionary<Rid, List<InstallationFile>>()
    {
      { Rid.win7_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("LOC server", "https://github.com/Fermat-ORG/iop-location-based-network/releases/download/1.0.0-alpha/iop-locnet-1.0.0-a1-win64.zip", @"%ProgramFiles%\IoP\LOC", true, "LocDir"),
      } },

      { Rid.win81_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("LOC server", "https://github.com/Fermat-ORG/iop-location-based-network/releases/download/1.0.0-alpha/iop-locnet-1.0.0-a1-win64.zip", @"%ProgramFiles%\IoP\LOC", true, "LocDir"),
      } },

      { Rid.win10_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("LOC server", "https://github.com/Fermat-ORG/iop-location-based-network/releases/download/1.0.0-alpha/iop-locnet-1.0.0-a1-win64.zip", @"%ProgramFiles%\IoP\LOC", true, "LocDir"),
      } },

      { Rid.ubuntu_14_04_x64, new List<InstallationFile>()
      {
        new AptGetInstallationFile("Google Protobuf v3 dependencies", "libc6 libgcc1 libstdc++6 zlib1g"),
        new DebPackageInstallationFile("Google Protobuf v3 package", "http://repo.fermat.community/pool/main/libp/libprotobuf10/libprotobuf10_3.0.0-b1-ubuntu1404_amd64.deb", "libprotobuf10"),

        new AptGetInstallationFile("SpatiaLite package", "libspatialite5"),

        new DebPackageInstallationFile("LOC server", "https://github.com/Fermat-ORG/iop-location-based-network/releases/download/1.0.0-alpha/iop-locnet_1.0.0-a1-ubuntu1404_amd64.deb", "iop-locnet"), 
      } },

      { Rid.ubuntu_16_04_x64, new List<InstallationFile>()
      {
        new AptGetInstallationFile("Google Protobuf v3 dependencies", "libc6 libgcc1 libstdc++6 zlib1g"),
        new DebPackageInstallationFile("Google Protobuf v3 package", "http://repo.fermat.community/pool/main/libp/libprotobuf10/libprotobuf10_3.0.0-b1-ubuntu1604_amd64.deb", "libprotobuf10"),

        new AptGetInstallationFile("SpatiaLite package", "libspatialite7"),

        new DebPackageInstallationFile("LOC server", "https://github.com/Fermat-ORG/iop-location-based-network/releases/download/1.0.0-alpha/iop-locnet_1.0.0-a1-ubuntu1604_amd64.deb", "iop-locnet"), 
      } },
    };

    /// <summary>List of configuration values mapped by system RIDs.</summary>
    private static Dictionary<Rid, Dictionary<string, string>> ConfigurationByRid = new Dictionary<Rid, Dictionary<string, string>>()
    {
      { Rid.win7_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\iop-locnet" },
        { "Configuration file", @"iop-locnet.cfg" },
        { "Database file", @"iop-locnet.sqlite" },
        { "Log file", @"iop-locnet.log" },
        { "Node port", "16980" },
        { "Client port", "16981" },
        { "Local port", "16982" },
      } },

      { Rid.win81_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\iop-locnet" },
        { "Configuration file", @"iop-locnet.cfg" },
        { "Database file", @"iop-locnet.sqlite" },
        { "Log file", @"iop-locnet.log" },
        { "Node port", "16980" },
        { "Client port", "16981" },
        { "Local port", "16982" },
      } },

      { Rid.win10_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\iop-locnet" },
        { "Configuration file", @"iop-locnet.cfg" },
        { "Database file", @"iop-locnet.sqlite" },
        { "Log file", @"iop-locnet.log" },
        { "Node port", "16980" },
        { "Client port", "16981" },
        { "Local port", "16982" },
      } },

      { Rid.ubuntu_14_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/.iop-locnet" },
        { "Configuration file", @"iop-locnet.cfg" },
        { "Database file", @"iop-locnet.sqlite" },
        { "Log file", @"iop-locnet.log" },
        { "Node port", "16980" },
        { "Client port", "16981" },
        { "Local port", "16982" },
      } },

      { Rid.ubuntu_16_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/.iop-locnet" },
        { "Configuration file", @"iop-locnet.cfg" },
        { "Database file", @"iop-locnet.sqlite" },
        { "Log file", @"iop-locnet.log" },
        { "Node port", "16980" },
        { "Client port", "16981" },
        { "Local port", "16982" },
      } },
    };

    /// <summary>LOC server configuration file template.</summary>
    private const string ConfigurationFileTemplate =
        "--nodeid $NODE_ID\n"
      + "--nodeport $NODE_PORT\n"
      + "--clientport $CLIENT_PORT\n"
      + "--localport $LOCAL_PORT\n"
      + "--latitude $LATITUDE\n"
      + "--longitude $LONGITUDE\n"
      + "--logpath \"$LOG_FILE\"\n"
      + "--dbpath \"$DB_FILE\"\n";


    /// <summary>Init.d template file name.</summary>
    private const string InitdScriptTemplateFile = "iop-loc";

    /// <summary>Name of Windows scheduled task.</summary>
    private const string WinTaskName = "IoP-LOC-Server";


    /// <summary>
    /// Initializes an instance of the component.
    /// </summary>
    public LocServer():
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

      string appDataDir = Environment.ExpandEnvironmentVariables(conf["Application data directory"]);

      while (!res)
      {
        string dataDir = InstallationFile.AskForEmptyDirectory(string.Format("Where do you want <white>LOC server's application data</white> to be stored? [{0}] ", appDataDir), appDataDir);
        conf["Application data directory"] = dataDir;
        GeneralConfiguration.SharedValues.Add("Loc-DataDir", dataDir);

        string logFile = Path.Combine(dataDir, conf["Log file"]);
        string dbFile = Path.Combine(dataDir, conf["Database file"]);
        string confFile = Path.Combine(dataDir, conf["Configuration file"]);
        GeneralConfiguration.SharedValues.Add("Loc-CfgFile", confFile);

        string locnetd = SystemInfo.CurrentRuntime.IsLinux() ? InstallationFile.Which("iop-locnetd") : Path.Combine(GeneralConfiguration.SharedValues["LocDir"], "iop-locnetd.exe");
        GeneralConfiguration.SharedValues[Name + "-executable"] = locnetd;
        GeneralConfiguration.SharedValues[Name + "-executable-args"] = string.Format("\"{0}\" --configfile \"{1}\"", locnetd, confFile);

        int localPort = int.Parse(conf["Local port"]);
        while (GeneralConfiguration.UsedPorts.ContainsKey(localPort))
          localPort++;
        conf["Local port"] = localPort.ToString();
        log.Debug("Selected port {0} as LOC server's local port.", localPort);
        GeneralConfiguration.UsedPorts.Add(localPort, "LOC server local interface");
        GeneralConfiguration.SharedValues.Add("LOC server local interface", localPort.ToString());


        int nodePort = int.Parse(conf["Node port"]);
        nodePort = AskForOpenPort(string.Format("Please enter a port number for <white>LOC server's node interface</white>. This port will have to be open and publicly accessible from the Internet. [{0}] ", nodePort), nodePort, "LOC server node interface");
        conf["Node port"] = nodePort.ToString();

        int clientPort = int.Parse(conf["Client port"]);
        clientPort = AskForOpenPort(string.Format("Please enter a port number for <white>LOC server's client interface</white>. This port will have to be open and publicly accessible from the Internet. [{0}] ", clientPort), clientPort, "LOC server client interface");
        conf["Client port"] = clientPort.ToString();


        Guid nodeId = Guid.NewGuid();
        string confFileContents = ConfigurationFileTemplate
          .Replace("$NODE_ID", nodeId.ToString())
          .Replace("$NODE_PORT", conf["Node port"])
          .Replace("$CLIENT_PORT", conf["Client port"])
          .Replace("$LOCAL_PORT", conf["Local port"])
          .Replace("$LATITUDE", GeneralConfiguration.Location.Latitude.ToString(CultureInfo.InvariantCulture))
          .Replace("$LONGITUDE", GeneralConfiguration.Location.Longitude.ToString(CultureInfo.InvariantCulture))
          .Replace("$LOG_FILE", logFile)
          .Replace("$DB_FILE", dbFile);

        CUI.WriteRich("Writing <white>LOC server's configuration</white> to <white>{0}</white>... ", confFile);
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
          { "{CFG}", GeneralConfiguration.SharedValues["Loc-CfgFile"] },
        };

        res = InstallationFile.InstallInitdScript(InitdScriptTemplateFile, templateReplacements, "start 99 2 3 4 5 . stop 1 0 1 6 .");
      }
      else if (SystemInfo.CurrentRuntime.IsWindows())
      {
        string bin = GeneralConfiguration.SharedValues[Name + "-executable"];
        string args = string.Format("--configfile \"{0}\"", GeneralConfiguration.SharedValues["Loc-CfgFile"]);
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
