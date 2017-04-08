using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServerInstaller
{
  public class ProfileServer : InstallableComponent
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.ProfileServer");

    /// <summary>Name of the component.</summary>
    private const string ComponentName = "Profile server";


    /// <summary>List of files that needs to be downloaded mapped by system RIDs.</summary>
    private static Dictionary<Rid, List<InstallationFile>> InstallationFilesByRid = new Dictionary<Rid, List<InstallationFile>>()
    {
      { Rid.win7_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("OpenSSL", "http://indy.fulgan.com/SSL/openssl-1.0.2k-x64_86-win64.zip", @"%ProgramFiles%\OpenSSL", true, "OpenSslDir"),
        new ZipArchiveInstallationFile("Profile server", "https://github.com/Fermat-ORG/iop-profile-server/releases/download/v1.0.1-alpha2/IoP-Profile-Server-v1.0.1-alpha2-Win7-2008R2-x64.zip", @"%ProgramFiles%\IoP\ProfileServer", true, "PsDir"),
      } },

      { Rid.win81_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("OpenSSL", "https://indy.fulgan.com/SSL/openssl-1.0.2k-x64_86-win64.zip", @"%ProgramFiles%\OpenSSL", true, "OpenSslDir"),
        new ZipArchiveInstallationFile("Profile server", "https://github.com/Fermat-ORG/iop-profile-server/releases/download/v1.0.1-alpha2/IoP-Profile-Server-v1.0.1-alpha2-Win81-2012R2-x64.zip", @"%ProgramFiles%\IoP\ProfileServer", true, "PsDir"),
      } },

      { Rid.win10_x64, new List<InstallationFile>()
      {
        new ZipArchiveInstallationFile("OpenSSL", "https://indy.fulgan.com/SSL/openssl-1.0.2k-x64_86-win64.zip", @"%ProgramFiles%\OpenSSL", true, "OpenSslDir"),
        new ZipArchiveInstallationFile("Profile server", "https://github.com/Fermat-ORG/iop-profile-server/releases/download/v1.0.1-alpha2/IoP-Profile-Server-v1.0.1-alpha2-Win10-2016-x64.zip", @"%ProgramFiles%\IoP\ProfileServer", true, "PsDir"),
      } },

      { Rid.ubuntu_14_04_x64, new List<InstallationFile>()
      {
        new AptGetInstallationFile("OpenSSL", "openssl"),
        new ZipArchiveInstallationFile("Profile server", "https://github.com/Fermat-ORG/iop-profile-server/releases/download/v1.0.1-alpha2/IoP-Profile-Server-v1.0.1-alpha2-Ubuntu-14.04-x64.zip", @"/usr/local/bin/iop-profile-server", true, "PsDir"),
      } },

      { Rid.ubuntu_16_04_x64, new List<InstallationFile>()
      {
        new AptGetInstallationFile("OpenSSL", "openssl"),
        new ZipArchiveInstallationFile("Profile server", "https://github.com/Fermat-ORG/iop-profile-server/releases/download/v1.0.1-alpha2/IoP-Profile-Server-v1.0.1-alpha2-Ubuntu-16.04-x64.zip", @"/usr/local/bin/iop-profile-server", true, "PsDir"),
      } },
    };


    /// <summary>List of configuration values mapped by system RIDs.</summary>
    private static Dictionary<Rid, Dictionary<string, string>> ConfigurationByRid = new Dictionary<Rid, Dictionary<string, string>>()
    {
      { Rid.win7_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\IoP-ProfileServer" },
        { "Primary port", "16987" },
        { "Other port", "16988" },
      } },

      { Rid.win81_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\IoP-ProfileServer" },
        { "Primary port", "16987" },
        { "Other port", "16988" },
      } },

      { Rid.win10_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%APPDATA%\IoP-ProfileServer" },
        { "Primary port", "16987" },
        { "Other port", "16988" },
      } },

      { Rid.ubuntu_14_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/.IoP-ProfileServer" },
        { "Primary port", "16987" },
        { "Other port", "16988" },
      } },

      { Rid.ubuntu_16_04_x64, new Dictionary<string, string>(StringComparer.Ordinal)
      {
        { "Application data directory", @"%HOME%/.IoP-ProfileServer" },
        { "Primary port", "16987" },
        { "Other port", "16988" },
      } },
    };


    /// <summary>Profile server configuration file template.</summary>
    private const string ConfigurationFileTemplate =
        "test_mode = off\n"
      + "external_server_address = $EXTERNAL_ADDR\n"
      + "bind_to_interface = 0.0.0.0\n"
      + "primary_interface_port = $PRIMARY_PORT\n"
      + "server_neighbor_interface_port = $NEIGHBOR_PORT\n"
      + "client_non_customer_interface_port = $NON_CUSTOMER_PORT\n"
      + "client_customer_interface_port = $CUSTOMER_PORT\n"
      + "client_app_service_interface_port = $APP_SERVICE_PORT\n"
      + "tls_server_certificate = $PFX_CERT_FILE\n"
      + "image_data_folder = $IMAGES_DIR\n"
      + "tmp_data_folder = $TMP_DIR\n"
      + "db_file_name = $DB_FILE\n"
      + "max_hosted_identities = 10000\n"
      + "max_identity_relations = 100\n"
      + "neighborhood_initialization_parallelism = 10\n"
      + "loc_port = $LOC_PORT\n"
      + "neighbor_profiles_expiration_time = 86400\n"
      + "max_neighborhood_size = 110\n"
      + "max_follower_servers_count = 200\n"
      + "follower_refresh_time = 43200\n"
      + "can_api_port = $CAN_API_PORT\n";


    /// <summary>
    /// Initializes an instance of the component.
    /// </summary>
    public ProfileServer():
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

        string dataDir = InstallationFile.AskForEmptyDirectory(string.Format("Where do you want <white>Profile server application data</white> to be stored? [{0}] ", appDataDir), appDataDir);
        conf["Application data directory"] = dataDir;

        string confFile = Path.Combine(GeneralConfiguration.SharedValues["PsDir"], "ProfileServer.conf");

        int primaryPort = int.Parse(conf["Primary port"]);
        primaryPort = AskForOpenPort(string.Format("Please enter a port number for <white>Profile server primary interface</white>. This port will have to be open and publicly accessible from the Internet. [{0}] ", primaryPort), primaryPort, "Profile server primary interface");
        conf["Primary port"] = primaryPort.ToString();

        int otherPort = int.Parse(conf["Other port"]);
        otherPort = AskForOpenPort(string.Format("Please enter a port number for <white>other interfaces of Profile server</white>. This port will have to be open and publicly accessible from the Internet. [{0}] ", otherPort), otherPort, "Profile server other interfaces");
        conf["Other port"] = otherPort.ToString();


        int canApiPort = int.Parse(GeneralConfiguration.SharedValues["CAN server API interface"]);
        int locLocalPort = int.Parse(GeneralConfiguration.SharedValues["LOC server local interface"]);

        string imagesDir = Path.Combine(dataDir, "images");
        string tmpDir = Path.Combine(dataDir, "tmp");
        string dbFile = Path.Combine(dataDir, "ProfileServer.db");
        string pfxFile = Path.Combine(dataDir, "ProfileServer.pfx");


        string confFileContents = ConfigurationFileTemplate
          .Replace("$EXTERNAL_ADDR", GeneralConfiguration.ExternalIpAddress.ToString())
          .Replace("$PRIMARY_PORT", conf["Primary port"])
          .Replace("$NEIGHBOR_PORT", conf["Other port"])
          .Replace("$NON_CUSTOMER_PORT", conf["Other port"])
          .Replace("$CUSTOMER_PORT", conf["Other port"])
          .Replace("$APP_SERVICE_PORT", conf["Other port"])
          .Replace("$PFX_CERT_FILE", pfxFile)
          .Replace("$IMAGES_DIR", imagesDir)
          .Replace("$TMP_DIR", tmpDir)
          .Replace("$DB_FILE", dbFile)
          .Replace("$TMP_DIR", tmpDir)
          .Replace("$LOC_PORT", locLocalPort.ToString())
          .Replace("$CAN_API_PORT", canApiPort.ToString());

        CUI.WriteRich("Writing <white>Profile server's configuration</white> to <white>{0}</white>... ", confFile);
        if (InstallationFile.CreateFileWriteTextChown(confFile, confFileContents))
        {
          CUI.WriteOk();

          done = true;

          bool error = true;
          string dbSourceFile = Path.Combine(GeneralConfiguration.SharedValues["PsDir"], "ProfileServer.db");
          if (string.Compare(dbFile, dbSourceFile, true) != 0)
          {
            CUI.WriteRich("Copying <white>Profile server's database file</white> to <white>{0}</white>... ", dbFile);
            try
            {
              File.Copy(dbSourceFile, dbFile);
              InstallationFile.Chown(dbFile);

              CUI.WriteOk();
              error = false;
            }
            catch (Exception e)
            {
              log.Error("Exception occurred: {0}", e.ToString());
              CUI.WriteFailed();
            }
          }

          if (!error)
          {
            CUI.WriteRich("Loading <white>Profile server's logging configuration file</white>... ");
            try
            {
              string nlogConfFile = Path.Combine(GeneralConfiguration.SharedValues["PsDir"], "NLog.config");
              string nlogConfFileContents = File.ReadAllText(nlogConfFile);
              CUI.WriteOk();

              nlogConfFileContents = nlogConfFileContents.Replace("${basedir}/Logs/", Path.Combine(dataDir, "Logs") + Path.DirectorySeparatorChar);
              CUI.WriteRich("Updating <white>Profile server's logging configuration file</white>... ");
              if (InstallationFile.CreateFileWriteTextChown(nlogConfFile, nlogConfFileContents))
              {
                CUI.WriteOk();

                CUI.WriteRich("Creating <white>Profile server's TLS certificate</white>... ");
                string openssl = SystemInfo.CurrentRuntime.IsLinux() ? "openssl" : Path.Combine(GeneralConfiguration.SharedValues["OpenSslDir"], "openssl.exe");
                if (InstallationFile.GeneratePfxCertificate(openssl, pfxFile))
                {
                  CUI.WriteOk();
                  res = true;
                }
                else
                {
                  log.Error("Failed to generate PFX certificate '{0}'.", pfxFile);
                  CUI.WriteFailed();
                }
              }
              else
              {
                log.Error("Failed to write NLog configuration to '{0}'.", nlogConfFile);
                CUI.WriteFailed();
              }
            }
            catch (Exception e)
            {
              log.Error("Exception occurred: {0}", e.ToString());
              CUI.WriteFailed();
            }
          }
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
