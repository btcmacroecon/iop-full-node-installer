using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FullNodeInstaller
{
  /// <summary>Possible statuses of the console process.</summary>
  public enum ConsoleProcessStatus
  {
    /// <summary>The process has been initialized and can be run.</summary>
    Initialized,

    /// <summary>The process has been started and is running.</summary>
    Running,

    /// <summary>The process has exited or has been terminated.</summary>
    Finished
  }

  /// <summary>
  /// Represents an external console process that can be run with a specified input 
  /// and then its output can be obtained.
  /// </summary>
  public class ConsoleProcess
  {
    /// <summary>Class logger.</summary>
    private static Logger log = LogManager.GetLogger("FullNodeInstaller.ConsoleProcess");

    /// <summary>Main process executable. May or may not include full path.</summary>
    private string executable;

    /// <summary>Command line arguments to run the process with.</summary>
    private string arguments;

    /// <summary>Console input for the new process.</summary>
    private byte[] inputData;

    /// <summary>Output of the started process.</summary>
    private List<string> outputData;

    /// <summary>Process object representing the console process.</summary>
    private Process process;

    /// <summary>Exit code of the process if it finished.</summary>
    private int processExitCode = -1;

    /// <summary>Current status of the process.</summary>
    public ConsoleProcessStatus Status;

    /// <summary>
    /// Creates a new instance of the object.
    /// </summary>
    /// <param name="Executable">Main process executable. May or may not include full path.</param>
    /// <param name="Arguments">Command line arguments to run the process with.</param>
    /// <param name="Input">Console input to be sent to the process once it is started.</param>
    public ConsoleProcess(string Executable, string Arguments = null, byte[] Input = null)
    {
      log.Trace("(Executable:'{0}',Arguments:'{1}')", Executable, Arguments);

      executable = Executable;
      arguments = Arguments;
      inputData = Input;
      Status = ConsoleProcessStatus.Initialized;

      log.Trace("(-)");
    }

    /// <summary>
    /// Creates a new instance of the object.
    /// </summary>
    /// <param name="Executable">Main process executable. May or may not include full path.</param>
    /// <param name="Arguments">Command line arguments to run the process with.</param>
    /// <param name="Input">Console input to be sent to the process once it is started.</param>
    public ConsoleProcess(string Executable, string Arguments, string Input) :
      this(Executable, Arguments, Input != null ? Encoding.UTF8.GetBytes(Input) : null)
    {
    }

    /// <summary>
    /// Creates a new instance of the object.
    /// </summary>
    /// <param name="Executable">Main process executable. May or may not include full path.</param>
    /// <param name="Arguments">Command line arguments to run the process with.</param>
    /// <param name="Input">Console input to be sent to the process once it is started.</param>
    public ConsoleProcess(string Executable, string Arguments, string[] Input) :
      this(Executable, Arguments, Input != null ? string.Join(Environment.NewLine, Input) : null)
    {
    }


    /// <summary>
    /// Starts the process.
    /// </summary>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public bool Run()
    {
      log.Trace("()");

      bool res = false;

      outputData = new List<string>();

      try
      {
        process = new Process();
        process.StartInfo.FileName = executable;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputHandler);
        process.ErrorDataReceived += new DataReceivedEventHandler(ProcessOutputHandler);
        process.EnableRaisingEvents = true;

        log.Debug("Starting process '{0}'...", GetCommandLine());
        if (process.Start())
        {
          Status = ConsoleProcessStatus.Running;
          log.Debug("Process is running.");

          try
          {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if ((inputData != null) && (inputData.Length > 0))
            {
              using (StreamWriter sw = new StreamWriter(process.StandardInput.BaseStream, Encoding.UTF8))
              {
                sw.Write(inputData);
              }
            }
          }
          catch (IOException e)
          {
            if (e.Message == "Broken pipe")
            {
              log.Trace("Process finished already.");
              Status = ConsoleProcessStatus.Finished;
            }
            else throw e;
          }

          res = true;
        }
      }
      catch (Exception e)
      {
        log.Trace("Exception occurred: {0}", e.ToString());

        if (Status == ConsoleProcessStatus.Running)
          KillProcess();

        Status = ConsoleProcessStatus.Finished;
      }

      log.Trace("(-):{0}", res);
      return res;
    }

    /// <summary>
    /// Terminates the process.
    /// </summary>
    public void KillProcess()
    {
      log.Trace("()");

      try
      {
        process.Kill();
      }
      catch
      {
      }

      log.Trace("(-)");
    }



    /// <summary>
    /// Standard output handler for console process.
    /// </summary>
    /// <param name="SendingProcess">Not used.</param>
    /// <param name="OutLine">Line of output without new line character.</param>
    public void ProcessOutputHandler(object SendingProcess, DataReceivedEventArgs OutLine)
    {
      if (OutLine.Data != null)
        outputData.Add(OutLine.Data);
    }


    /// <summary>
    /// Waits for the process termination. If the process does not finish on time, it is terminated.
    /// </summary>
    /// <param name="TimeoutMs">Number of milliseconds to wait for the process exit before it is considered as failed and the process is terminated.</param>
    /// <returns>true if the function succeeds, false otherwise.</returns>
    public bool WaitExit(int TimeoutMs = 15000)
    {
      log.Trace("(TimeoutMs:{0})", TimeoutMs);

      bool res = false;
      if (Status != ConsoleProcessStatus.Running) return res;

      try
      {
        res = process.WaitForExit(TimeoutMs);
        processExitCode = process.ExitCode;
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      if (!res) KillProcess();

      Status = ConsoleProcessStatus.Finished;

      log.Trace("(-):{0}", res);
      return res;
    }


    /// <summary>
    /// Waits for the process termination. If the process does not finish on time, it is terminated.
    /// </summary>
    /// <param name="TimeoutMs">Number of milliseconds to wait for the process exit before it is considered as failed and the process is terminated.</param>
    /// <returns>true if the function succeeds, which means that the process finished on time with exit code 0, false otherwise.</returns>
    public bool WaitSuccessExit(int TimeoutMs = 5000)
    {
      bool res = false;

      if (WaitExit(TimeoutMs))
      {
        res = HasSuccessExitCode();
        if (!res)
          log.Debug("Process exit code was {0}, its output follows:\n---------------------------------\n{1}\n---------------------------------\n", processExitCode, string.Join("\n", outputData));
      }

      return res;
    }

    /// <summary>
    /// Runs the process and waits for the process termination. If the process does not finish on time, it is terminated.
    /// </summary>
    /// <param name="TimeoutMs">Number of milliseconds to wait for the process exit before it is considered as failed and the process is terminated.</param>
    /// <returns>true if the function succeeds, which means that the process was started successfully and finished on time with exit code 0, false otherwise.</returns>
    public bool RunAndWaitSuccessExit(int TimeoutMs = 5000)
    {
      return Run() && WaitSuccessExit(TimeoutMs);
    }


    /// <summary>
    /// Retrieves the process output.
    /// </summary>
    /// <returns>Process output as a list of output console lines without line endings.</returns>
    public List<string> GetOutput()
    {
      return outputData;
    }

    /// <summary>
    /// Retrieves the process exit code.
    /// </summary>
    /// <returns>Process exit code.</returns>
    public int GetProcessExitCode()
    {
      return processExitCode;
    }

    /// <summary>
    /// Checks whether the process returned zero on its exit.
    /// </summary>
    /// <returns>true if the process returned zero on its exit, false otherwise.</returns>
    public bool HasSuccessExitCode()
    {
      return processExitCode == 0;
    }

    /// <summary>
    /// Returns the command line of the process including arguments.
    /// </summary>
    /// <returns>Command line of the process including arguments.</returns>
    public string GetCommandLine()
    {
      return !string.IsNullOrEmpty(arguments) ? string.Format("{0} {1}", executable, arguments) : executable;
    }
  }
}
