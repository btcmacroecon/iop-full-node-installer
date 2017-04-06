using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ServerInstaller
{
  public abstract class DownloadableInstallationFile : InstallationFile
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.DownloadableInstallationFile");

    /// <summary>URI to download the installation file from.</summary>
    protected string uri;

    /// <summary>Name of the downloaded file.</summary>
    protected string downloadedFileName;

    /// <summary>
    /// Creates a new instance of the class.
    /// </summary>
    /// <param name="Name">Human readable name of the installation file.</param>
    /// <param name="Uri">URI to download the installation file from.</param>
    /// <param name="CanUninstall">If true, the file can be uninstalled.</param>
    public DownloadableInstallationFile(string Name, string Uri, bool CanUninstall = true) :
      base(Name, CanUninstall)
    {
      this.uri = Uri;
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

      if (Status.HasFlag(InstallationFileStatus.Downloaded))
      {
        CUI.WriteRich("Deleting downloaded file of <white>{0}</white>... ", Name);
        if (DeleteFile(downloadedFileName))
        {
          Status &= ~InstallationFileStatus.Downloaded;
          CUI.WriteOk();
        }
        else
        {
          log.Error("Failed to delete file '{0}'.", downloadedFileName);
          CUI.WriteFailed();
          error = true;
        }
      }

      res = !error;

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Downloads a file to a temporary folder.
    /// </summary>
    /// <param name="Uri">URI of the file to download.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public bool DownloadFileToTemp(string Uri)
    {
      log.Trace("()");
      bool res = false;

      Uri uri = new Uri(Uri);
      string fileName = GenerateTemporaryFileName(uri);

      log.Debug("Downloading '{0}' to '{1}', attempt 1 ...", uri, fileName);
      try
      {
        using (HttpClient client = new HttpClient())
        {
          client.Timeout = TimeSpan.FromSeconds(360);

          using (HttpResponseMessage message = client.GetAsync(uri).Result)
          {
            if (message.IsSuccessStatusCode)
            {
              byte[] data = message.Content.ReadAsByteArrayAsync().Result;
              File.WriteAllBytes(fileName, data);
              res = true;
            }
            else log.Error("Downloading failed with status code {0}.", message.StatusCode);
          }
        }
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }


      if (!res)
      {
        log.Debug("Downloading failed, trying again with different method.");
        byte[] data = DownloadFileRaw(Uri);
        if (data != null)
        {
          File.WriteAllBytes(fileName, data);
          res = true;
        }
        else log.Error("Downloading failed.");
      }

      if (res)
      {
        if (GetFullFileName(fileName, out downloadedFileName)) Status |= InstallationFileStatus.Downloaded;
        else log.Error("File '{0}' not found.", fileName);
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Tries to download a file from HTTP server using TcpClient.
    /// </summary>
    /// <param name="FileUri">URI of the file to download.</param>
    /// <returns>File data or null if the function fails.</returns>
    public byte[] DownloadFileRaw(string FileUri)
    {
      log.Trace("(FileUri:'{0}')", FileUri);
      byte[] res = null;
      TcpClient client = null;
      NetworkStream stream = null;
      try
      {
        Uri uri = new Uri(FileUri);
        string host = uri.Host;
        string query = uri.PathAndQuery;

        string request = string.Format(
          "GET {0} HTTP/1.1\r\n" 
          + "Host: {1}\r\n" 
          + "Connection: close\r\n" 
          + "\r\n", query, host);

        client = new TcpClient();
        client.NoDelay = true;
        client.ConnectAsync(host, 80).Wait();

        stream = client.GetStream();

        byte[] reqBytes = Encoding.UTF8.GetBytes(request);
        stream.Write(reqBytes, 0, reqBytes.Length);

        BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8);

        string response = "";
        string line;
        char c;

        do
        {
          line = "";
          c = '\u0000';
          while (true)
          {
            c = binaryReader.ReadChar();
            if (c == '\n')
              break;
            line += c;
          }
          response += line.Trim() + "\r\n";
        } while (line.Trim().Length > 0);

        Regex contentLengthRegex = new Regex(@"(?<=Content-Length:\s)\d+", RegexOptions.IgnoreCase);
        int contentLength = int.Parse(contentLengthRegex.Match(response).Value);
        log.Trace("Content length is {0}.", contentLength);

        using (MemoryStream memStream = new MemoryStream(contentLength))
        {
          byte[] buffer = new byte[8192];
          int total = 0;
          int read = 0;

          while (total < contentLength)
          {
            if (stream.DataAvailable)
            {
              read = stream.Read(buffer, 0, buffer.Length);
              total += read;
              memStream.Write(buffer, 0, read);
            }
          }

          res = memStream.ToArray();
        }
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      if (stream != null) stream.Dispose();
      if (client != null) client.Dispose();

      if (res != null) log.Trace("(-):*.Length={0}", res.Length);
      else log.Trace("(-):null");
      return res;
    }
  }
}
