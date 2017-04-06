using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ServerInstaller
{
  public class LocServer : InstallableComponent
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.LocServer");

    /// <summary>Name of the component.</summary>
    private const string ComponentName = "LOC server";


    /// <summary>List of files that needs to be downloaded mapped by system RIDs.</summary>
    private static Dictionary<Rid, List<InstallationFile>> InstallationFilesByRid = new Dictionary<Rid, List<InstallationFile>>()
    {
      { Rid.win7_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("LOC server", "https://github.com/Fermat-ORG/iop-location-based-network/releases/download/1.0.0-alpha/iop-locnet-1.0.0-a1-win64.zip", @"%ProgramFiles%\IoP\LOC"),
      } },

      { Rid.win81_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("LOC server", "https://github.com/Fermat-ORG/iop-location-based-network/releases/download/1.0.0-alpha/iop-locnet-1.0.0-a1-win64.zip", @"%ProgramFiles%\IoP\LOC"),
      } },

      { Rid.win10_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("LOC server", "https://github.com/Fermat-ORG/iop-location-based-network/releases/download/1.0.0-alpha/iop-locnet-1.0.0-a1-win64.zip", @"%ProgramFiles%\IoP\LOC"),
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
        { "Application data directory", @"%HOME%/iop-locnet" },
        { "Configuration file", @"iop-locnet.cfg" },
        { "Database file", @"iop-locnet.sqlite" },
        { "Log file", @"iop-locnet.log" },
        { "Node port", "16980" },
        { "Client port", "16981" },
        { "Local port", "16982" },
      } },

      { Rid.ubuntu_16_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/iop-locnet" },
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

        string logFile = Path.Combine(dataDir, conf["Log file"]);
        string dbFile = Path.Combine(dataDir, conf["Database file"]);
        string confFile = Path.Combine(dataDir, conf["Configuration file"]);

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
  }
}
