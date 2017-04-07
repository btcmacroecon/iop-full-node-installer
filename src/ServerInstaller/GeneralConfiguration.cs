using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;

namespace ServerInstaller
{
  /// <summary>
  /// Obtains and manages configuration values common for multiple components.
  /// </summary>
  public static class GeneralConfiguration
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("ServerInstaller.Program");

    /// <summary>Name of the script on seed nodes that will reveal IP address.</summary>
    public const string ExternalIpUri = "{0}/getip";

    /// <summary>Name of the script on seed nodes that will check whether a TCP port is controlled by the client.</summary>
    public const string PortCheckUri = "{0}/portcheck?ip={1}&port={2}";

    /// <summary>Name of the script on seed nodes that will provide location information about the IP address.</summary>
    public const string LocationUri = "{0}/location?ip={1}";


    /// <summary>IP address of the external interface that can be used to contact the servers on this machine.</summary>
    public static IPAddress ExternalIpAddress;

    /// <summary>GPS location of this machine.</summary>
    public static GpsLocation Location;

    /// <summary>List of values other components wanted to store for later reuse.</summary>
    public static Dictionary<string, string> SharedValues = new Dictionary<string, string>(StringComparer.Ordinal);


    /// <summary>List of seed node servers with helper scripts.</summary>
    public static List<string> SeedNodes = new List<string>()
    {
      "http://ham1.fermat.cloud:9090",
      "http://ham2.fermat.cloud:9090",
      "http://ham3.fermat.cloud:9090",
      "http://ham4.fermat.cloud:9090",
    };


    /// <summary>List of ports that are occupied and can not be used. Key is the port number, value is description of the service that is using the port.</summary>
    public static Dictionary<int, string> UsedPorts = new Dictionary<int, string>();


    /// <summary>Random seed.</summary>
    private static Random rng = new Random();

    /// <summary>
    /// Initializes the component.
    /// </summary>
    static GeneralConfiguration()
    {
      // Randomly shuffle SeedNodes.
      Shuffle(SeedNodes);
    }

    /// <summary>
    /// Randomly shuffles list.
    /// </summary>
    /// <param name="list">List to shuffle.</param>
    public static void Shuffle<T>(this List<T> list)
    {
      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = rng.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }



    /// <summary>
    /// Performs HTTP GET request to a server and returns a response.
    /// </summary>
    /// <param name="Uri">URI to send HTTP GET request to.</param>
    /// <returns>Body of the response if the function succeeds and the server sends HTTP 200 OK response, null otherwise.</returns>
    private static string HttpGet(string Uri)
    {
      log.Trace("(Uri:'{0}')", Uri);

      string res = null;
      try
      {
        Uri uri = new Uri(Uri);

        using (HttpClient client = new HttpClient())
        {
          client.Timeout = TimeSpan.FromSeconds(15);

          using (HttpResponseMessage message = client.GetAsync(uri).Result)
          {
            if (message.IsSuccessStatusCode)
            {
              string data = message.Content.ReadAsStringAsync().Result;
              res = data;
            }
            else log.Error("Downloading failed with status code {0}.", message.StatusCode);
          }
        }
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      log.Trace("(-):\n{0}", res);
      return res;
    }

    
    /// <summary>
    /// Connects to a seed node and obtains information about external IP address.
    /// </summary>
    /// <returns>IP address of an external interface.</returns>
    public static IPAddress FindExternalIpAddress()
    {
      log.Trace("()");

      IPAddress res = null;

      foreach (string seedNode in SeedNodes)
      {
        string uri = string.Format(ExternalIpUri, seedNode);

        log.Trace("Sending request to '{0}'.", uri);

        string response = HttpGet(uri);
        if (response != null)
        {
          response = response.Trim();
          IPAddress val;
          if (IPAddress.TryParse(response, out val))
          {
            ExternalIpAddress = val;
            res = ExternalIpAddress;
            break;
          }
          else log.Error("Received invalid response from '{0}'.", uri);
        }
        else log.Error("Request to '{0}' failed.", uri);
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Connects to a seed node and obtains IP address location information.
    /// </summary>
    /// <param name="IpAddress">IP address to find location for.</param>
    /// <returns>Estimated GPS location of the machine or null if the funtion fails.</returns>
    public static GpsLocation FindIpLocation(IPAddress IpAddress)
    {
      log.Trace("(IpAddress:{0})", IpAddress);

      GpsLocation res = null;

      foreach (string seedNode in SeedNodes)
      {
        string uri = string.Format(LocationUri, seedNode, IpAddress);

        log.Trace("Sending request to '{0}'.", uri);

        string response = HttpGet(uri);
        if (response != null)
        {
          response = response.Trim();
          string[] parts = response.Split(new char[] { ';', ',' });

          if (parts.Length == 2)
          {
            decimal lat;
            decimal lon;
            if (decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out lat)
              && decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out lon))
            {
              GpsLocation location = new GpsLocation(lat, lon);

              if (location.IsValid()) res = location;
              break;
            }
          }

          log.Error("Invalid response received from '{0}':\n{1}", uri, response);
        }
        else log.Error("Request to '{0}' failed.", uri);
      }

      log.Trace("(-):{0}", res);
      return res;
    }



    /// <summary>
    /// Asks a seed node to verify that a certain port is open.
    /// </summary>
    /// <param name="IpAddress">IP address on which the port should be open.</param>
    /// <param name="Port">TCP port which should be open.</param>
    /// <returns>true if the port was checked and it is open, false otherwise.</returns>
    public static bool CheckPort(IPAddress IpAddress, int Port)
    {
      log.Trace("(IpAddress:{0},Port:{1})", IpAddress, Port);

      bool res = false;
      foreach (string seedNode in SeedNodes)
      {
        string uri = string.Format(PortCheckUri, seedNode, IpAddress, Port);

        log.Trace("Sending request to '{0}'.", uri);

        string response = HttpGet(uri);
        if (response != null)
        {
          response = response.Trim();
          if (response == "OK")
          {
            res = true;
            break;
          }
          else if (response == "FAILED")
          {
            res = false;
            break;
          }

          log.Error("Invalid response received from '{0}':\n{1}", uri, response);
        }
        else log.Error("Request to '{0}' failed.", uri);
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Description of result provided by ipinfo.io.
    /// </summary>
    public class IpinfoIoResult
    {
      /// <summary>IP address for which the info is provided.</summary>
      public string ip;

      /// <summary>Reverse name of the IP address.</summary>
      public string hostname;

      /// <summary>City where the IP address is hosted.</summary>
      public string city;

      /// <summary>Region where the IP address is hosted.</summary>
      public string region;

      /// <summary>Country where the IP address is hosted.</summary>
      public string country;

      /// <summary>Latitude and longitude where the IP address is hosted.</summary>
      public string loc;

      /// <summary>Organization unit to which the IP belong.</summary>
      public string org;

      /// <summary>Postal code where the IP address is hosted.</summary>
      public string postal;

      /// <summary>Phone number to responsible person for the IP address.</summary>
      public string phone;
    }

    /// <summary>URI of the service that provides IP location information.</summary>
    public const string LocationServiceUri = "http://ipinfo.io/{0}/json";

    /// <summary>
    /// Connects to a third party service ipinfo.io and obtains information about IP address location.
    /// </summary>
    /// <param name="IpAddress">IP address to find location for.</param>
    /// <returns>Estimated GPS location of the machine or null if the funtion fails.</returns>
    public static GpsLocation FindIpLocationIpinfoIo(IPAddress IpAddress)
    {
      log.Trace("(IpAddress:{0})", IpAddress);

      GpsLocation res = null;

      string uri = string.Format(LocationServiceUri, IpAddress);

      log.Trace("Sending request to '{0}'.", uri);

      string response = HttpGet(uri);
      if (response != null)
      {
        try
        {
          IpinfoIoResult ipInfo = JsonConvert.DeserializeObject<IpinfoIoResult>(response);
          if (ipInfo.loc != null)
          {
            string[] parts = ipInfo.loc.Split(new char[] { ',', ';' });
            if (parts.Length == 2)
            {
              decimal lat;
              decimal lon;
              if (decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out lat)
                && decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out lon))
              {
                GpsLocation location = new GpsLocation(lat, lon);

                if (location.IsValid()) res = location;
              }
            }
          }

          if (res == null) log.Error("Invalid location information '{0}' received.", ipInfo.loc);
        }
        catch
        {
          log.Error("Received invalid response from '{0}':\n{1}", uri, response);
        }
      }
      else log.Error("Request to '{0}' failed.", uri);

      log.Trace("(-):{0}", res);
      return res;
    }

  }
}
