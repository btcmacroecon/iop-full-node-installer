using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FullNodeInstaller
{
  /// <summary>Possible states of the installable component.</summary>
  [Flags]
  public enum InstallableComponentStatus
  {
    /// <summary>The component has not been installed yet, or has been uninstalled.</summary>
    None = 0,

    /// <summary>The component installation has been started.</summary>
    InstallationInitiated = 1,

    /// <summary>The component installation has been successfully finished.</summary>
    InstallationCompleted = 2,

    /// <summary>Autorun for this component has been installed.</summary>
    AutorunInstalled = 4,

    /// <summary>This component has been started and is running.</summary>
    Running = 8
  }


  /// <summary>
  /// Base class for all servers and components that are to be installed.
  /// </summary>
  public abstract class InstallableComponent
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("FullNodeInstaller.InstallableComponent");

    /// <summary>Name of the component.</summary>
    public string Name;

    /// <summary>Status of the component.</summary>
    public InstallableComponentStatus Status = InstallableComponentStatus.None;


    private List<InstallationFile> installationFiles;

    /// <summary>
    /// Obtains an installation file of the installable component by its runtime identifier.
    /// </summary>
    /// <returns>List of descriptions of the installation files in their installation order or null if the function fails.</returns>
    public abstract List<InstallationFile> GetInstallationFiles();


    /// <summary>
    /// Configures the component.
    /// </summary>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public abstract bool Configure();

    /// <summary>
    /// Installs the component so that it is automatically started during the operating system start.
    /// </summary>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public abstract bool AutorunSetup();

    /// <summary>
    /// Uninstalls the autorun setup of the component.
    /// </summary>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public abstract bool AutorunSetupUninstall();

    /// <summary>
    /// Starts the component.
    /// </summary>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public abstract bool Start();

    /// <summary>
    /// Stops the component.
    /// </summary>
    /// <returns>true if the function succeeded, false otherwise.</returns>
    public abstract bool Stop();


    /// <summary>
    /// Initializes an instance of the component.
    /// </summary>
    /// <param name="Name">Name of the component.</param>
    public InstallableComponent(string Name)
    {
      this.Name = Name;
    }


    /// <summary>
    /// Installs all installation files of the component.
    /// </summary>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public virtual bool Install()
    {
      log.Trace("()");
      bool res = false;

      Status = InstallableComponentStatus.InstallationInitiated;

      bool error = false;
      installationFiles = GetInstallationFiles();
      foreach (InstallationFile instFile in installationFiles)
      {
        log.Info("Installing '{0}'.", instFile.Name);
        CUI.WriteRich("Installing module <white>{0}</white>.\n", instFile.Name);

        if (!instFile.Install())
        {
          log.Error("Installation of '{0}' failed.", instFile.Name);
          error = true;
          break;
        }
      }

      if (!error)
      {
        Status |= InstallableComponentStatus.InstallationCompleted;
        res = true;
      }
      else
      {
        // Cleanup.
        foreach (InstallationFile instFile in installationFiles)
        {
          if (instFile.Status != InstallationFileStatus.None)
          {
            log.Info("Uninstalling '{0}'.", instFile.Name);
            instFile.Uninstall();
          }
        }
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Uninstalls the component.
    /// </summary>
    public void Uninstall()
    {
      log.Trace("()");

      if (Status.HasFlag(InstallableComponentStatus.Running))
        Stop();

      if (Status.HasFlag(InstallableComponentStatus.AutorunInstalled))
        AutorunSetupUninstall();

      foreach (InstallationFile instFile in installationFiles)
      {
        if (instFile.Status != InstallationFileStatus.None)
        {
          log.Info("Uninstalling '{0}'.", instFile.Name);
          instFile.Uninstall();
        }
      }

      Status &= ~InstallableComponentStatus.InstallationCompleted;

      log.Trace("(-)");
    }


    /// <summary>
    /// Asks user to input a TCP port number and verifies that it is an open port.
    /// </summary>
    /// <param name="RichMessage">Message to print on the console.</param>
    /// <param name="DefaultValue">Default value for the port.</param>
    /// <param name="ServiceName">Name of the service this port will be used by.</param>
    /// <param name="AllowCancel">Allow user to cancel port selection.</param>
    /// <returns>Selected port value that is open from outside.</returns>
    public int AskForOpenPort(string RichMessage, int DefaultValue, string ServiceName, bool AllowCancel = false)
    {
      log.Trace("(DefaultValue:{0},ServiceName:'{1}',AllowCancel:{2})", DefaultValue, ServiceName, AllowCancel);

      int res = 0;

      bool done = false;
      while (!done)
      {
        CUI.WriteRich(RichMessage);
        string answer = CUI.ReadStringAnswer(DefaultValue.ToString());

        int port;
        if (int.TryParse(answer, out port) && (0 < port) && (port <= 65535))
        {
          if (!GeneralConfiguration.UsedPorts.ContainsKey(port))
          {
            if (CheckPortOpenUI(port))
            {
              GeneralConfiguration.UsedPorts[port] = ServiceName;
              res = port;
              done = true;
            }
            else log.Warn("Port {0} is not open.", port);
          }
          else
          {
            log.Error("Port {0} is already used for '{1}'.", port, GeneralConfiguration.UsedPorts[port]);
            CUI.WriteRich("<red>ERROR:</red> Port <white>{0}</white> is already used by <white>{1}</white>. Please try different port.\n", port, GeneralConfiguration.UsedPorts[port]);
          }
        }
        else if (AllowCancel && (answer == "0"))
        {
          log.Debug("Port selection cancelled.");
          done = true;
        }
        else 
        {
          log.Error("Invalid port number entered '{0}'.", answer);
          CUI.WriteRich("<red>ERROR:</red> <white>'{0}'</white> is not a valid port number. It has to be an integer between 1 and 65535. Please try again.\n", answer);
        }
      }

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Asks user to open a specific port, then checks it.
    /// </summary>
    /// <param name="Port">TCP port to check.</param>
    /// <returns>true if the port is open, false if user want to otherwise.</returns>
    public bool CheckPortOpenUI(int Port)
    {
      log.Trace("(Port:{0})", Port);

      bool res = false;

      bool done = false;
      while (!done)
      {
        CUI.WriteRich("Checking whether the TCP port <white>{0}</white> can be accessed from the Internet by connecting to <white>{1}:{0}</white>... ", Port, GeneralConfiguration.ExternalIpAddress);

        if (CheckPortOpen(Port))
        {
          res = true;
          break;
        }

        CUI.WriteRich("TCP port <white>{0}</white> is not open or an error occurred. How would you like to proceed? [<white>C</white>HECK AGAIN / <white>s</white>elect different port] ", Port, GeneralConfiguration.ExternalIpAddress);

        done = CUI.ReadKeyAnswer(new char[] { 'c', 's' }) == 's';
      }

      CUI.WriteLine();

      log.Trace("(-):{0}", res);
      return res;
    }

    /// <summary>
    /// Tries to listen on a specific port and asks seed node to connect to it.
    /// </summary>
    /// <param name="Port">TCP port to check.</param>
    /// <returns>true if the port is open, false otherwise.</returns>
    public bool CheckPortOpen(int Port)
    {
      log.Trace("(Port:{0})", Port);

      bool res = false;

      if (Program.TestMode)
      {
        if (Port > 50000)
        {
          CUI.WriteOk();
          res = true;
          log.Trace("(-)[TEST_MODE_ENABLED]:{0}", res);
          return res;
        }
      }

      try
      {
        portListenerThreadShutdownEvent.Reset();

        Thread portListenerThread = new Thread(new ParameterizedThreadStart(PortListenerThread));
        portListenerThread.Start(Port);

        if (portListenerReadyEvent.WaitOne(5000))
        {
          res = GeneralConfiguration.CheckPort(GeneralConfiguration.ExternalIpAddress, Port);
          if (res) CUI.WriteOk();
          else CUI.WriteFailed();
        }
        else
        {
          log.Error("Port listener thread did not get ready on time.");
          CUI.WriteFailed();
          CUI.WriteRich("<red>ERROR:</red> Unexpected error occurred, please try again.\n");
        }

        portListenerThreadShutdownEvent.Set();

        if (!portListenerThread.Join(10000))
          log.Error("Port listener thread failed to finish on time.");
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
        CUI.WriteFailed();
        CUI.WriteRich("<red>ERROR:</red> Exception occurred: {0}\n", e.Message);
      }


      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>Event that is used to shutdown PortListenerThread.</summary>
    private static ManualResetEvent portListenerThreadShutdownEvent = new ManualResetEvent(false);

    /// <summary>Event that is used to signal that PortListenerThread is ready to accept clients.</summary>
    private static ManualResetEvent portListenerReadyEvent = new ManualResetEvent(false);


    /// <summary>
    /// Thread procedure that is responsible for accepting new clients on the TCP server port.
    /// </summary>
    private void PortListenerThread(object State)
    {
      int port = (int)State;
      log.Trace("(Port:{0})", port);

      try
      {
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Server.LingerState = new LingerOption(true, 0);
        listener.Server.NoDelay = true;
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();

        portListenerReadyEvent.Set();

        AutoResetEvent acceptTaskEvent = new AutoResetEvent(false);
        bool done = false;
        while (!done)
        {

          log.Debug("Waiting for new client.");
          Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
          acceptTask.ContinueWith(t => acceptTaskEvent.Set());

          WaitHandle[] handles = new WaitHandle[] { acceptTaskEvent, portListenerThreadShutdownEvent };
          int index = WaitHandle.WaitAny(handles);
          if (handles[index] == portListenerThreadShutdownEvent)
          {
            log.Trace("Shutdown event detected.");
            break;
          }

          TcpClient client = null;
          NetworkStream stream = null;
          try
          {
            client = acceptTask.Result;
            stream = client.GetStream();

            byte[] buffer = new byte[1024];
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            if (byteCount > 0)
            {
              string dataStr = Encoding.UTF8.GetString(buffer, 0, byteCount);
              log.Trace("Received {0} bytes of data:\n{1}", byteCount, dataStr);

              log.Trace("Sending the data back to client.");
              stream.Write(buffer, 0, byteCount);
              stream.Flush();
              Thread.Sleep(100);
            }
            else log.Trace("Connection to client has been terminated.");
          }
          catch (Exception e)
          {
            log.Error("Exception occurred: {0}", e.ToString());
          }

          if (stream != null) stream.Dispose();
          if (client != null) client.Dispose();
        }

        log.Trace("Stopping listener.");
        listener.Stop();
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      log.Trace("(-)");
    }

  }
}
