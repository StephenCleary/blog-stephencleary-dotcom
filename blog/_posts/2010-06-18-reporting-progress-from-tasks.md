---
layout: post
title: "Reporting Progress from Tasks"
tags: ["Threading", ".NET"]
---
<div style="background-color:#eee">
  <b>Update 2012-02-16: The information in this post is old. See the new post <a href="http://blog.stephencleary.com/2012/02/reporting-progress-from-async-tasks.html">Reporting Progress from Async Tasks</a> for a better solution.</b>
</div>



The .NET 4.0 [Task Parallel Library](http://msdn.microsoft.com/en-us/library/dd460717.aspx) respresents a huge shift in the way future multithreaded code will be written. The TPL and higher-level abstractions (such as the [Parallel class](http://msdn.microsoft.com/en-us/library/system.threading.tasks.parallel.aspx), [Parallel LINQ](http://msdn.microsoft.com/en-us/library/dd460688.aspx), and the [Reactive Extensions](http://msdn.microsoft.com/en-us/devlabs/ee794896.aspx)) will (hopefully) become the default approach for handling all multithreading situations. There is (almost) no reason to use the old [Thread class](http://msdn.microsoft.com/en-us/library/system.threading.thread.aspx) anymore.





Similarly, the [BackgroundWorker class](http://msdn.microsoft.com/en-us/library/system.componentmodel.backgroundworker.aspx) has seen its heyday. It is time for this old class to retire as well. However, BGW does have one benefit over the TPL: it is easier to use for background tasks that need to report progress to the UI.





Background tasks come in two basic flavors. Some of them update the UI infrequently, and can be easily broken into separate tasks which only update at each "checkpoint." Other background tasks update the UI more frequently, and cannot be easily divided this way.





For the background tasks that infrequently update the UI, the common approach is to split them into separate Tasks with "checkpoints" in-between. At these "checkpoints," a [task continuation](http://msdn.microsoft.com/en-us/library/ee372288.aspx) is used to update the UI. The C# FAQ blog has [an entry](http://blogs.msdn.com/b/csharpfaq/archive/2010/06/18/parallel-programming-task-schedulers-and-synchronization-context.aspx) describing this approach.





For the background tasks that need to frequently update the UI (and can't be easily split into "checkpointed" Tasks), another approach is necessary. The easiest solution is to create an inner Task to update the UI.





This post introduces the ProgressReporter type, which greatly simplifies background tasks that need to do frequent progress reporting. The goal for ProgressReporter is to allow update code that is as simple as [BackgroundWorker.ProgressChanged](http://msdn.microsoft.com/en-us/library/system.componentmodel.backgroundworker.progresschanged.aspx).



## The Example Framework



The UI is a single form with 3 buttons and a progress bar. The three buttons are Start, Error, and Cancel. The buttons are enabled and disabled based on whether the background task is running. The progress bar shows the progress of the task.





The background task runs for 3 seconds, counting from 0 to 99, updating the progress bar every 30 milliseconds. The task will then produce a result of 42. If the Error button is used to start the task, then the task will throw an exception instead of producing a result. The task is also cancelable, checking for cancellation each time it reports progress.





This is a rather complex example; it covers each background task scenario (successful completion, error conditions, and cancellation).





The UI framework is the same regardless of whether a BackgroundWorker or Task object is used for the background task:




using System;
using System.Windows.Forms;

public sealed partial class MainForm : Form
{
  private readonly Button startButton;
  private readonly Button errorButton;
  private readonly Button cancelButton;
  private readonly ProgressBar progressBar;

  public MainForm()
  {
    this.startButton = new Button
    {
      Text = "Start",
      Height = 23, Width = 75,
      Left = 12, Top = 12,
    };
    this.errorButton = new Button
    {
      Text = "Error",
      Height = 23, Width = 75,
      Left = this.startButton.Right + 6,
      Top = 12,
    };
    this.cancelButton = new Button
    {
      Text = "Cancel",
      Enabled = false,
      Height = 23, Width = 75,
      Left = this.errorButton.Right + 6,
      Top = 12,
    };
    this.progressBar = new ProgressBar
    {
      Width = this.cancelButton.Right - 12,
      Height = 23,
      Left = 12,
      Top = this.startButton.Bottom + 6,
    };
    this.startButton.Click +=
      (sender, e) => this.startButton_Click(sender, e);
    this.errorButton.Click +=
      (sender, e) => this.errorButton_Click(sender, e);
    this.cancelButton.Click +=
      (sender, e) => this.cancelButton_Click(sender, e);
    this.Controls.AddRange(new Control[]
    {
      this.startButton,
      this.errorButton,
      this.cancelButton,
      this.progressBar,
    });
  }

  partial void startButton_Click(object sender, EventArgs e);
  partial void errorButton_Click(object sender, EventArgs e);
  partial void cancelButton_Click(object sender, EventArgs e);

  private void TaskIsRunning()
  {
    // Update UI to reflect background task.
    this.startButton.Enabled = false;
    this.errorButton.Enabled = false;
    this.cancelButton.Enabled = true;
  }

  private void TaskIsComplete()
  {
    // Reset UI.
    this.progressBar.Value = 0;
    this.startButton.Enabled = true;
    this.errorButton.Enabled = true;
    this.cancelButton.Enabled = false;
  }
}

class Program
{
  [STAThread]
  static void Main()
  {
    // Run the UI.
    Application.Run(new MainForm());
  }
}




This defines a form called MainForm that has the UI described above. The two methods TaskIsRunning and TaskIsComplete handle the enabling and disabling of the buttons. There are also partial methods as placeholders for the button click events; these are used by the sample code below.





You can copy the code above by double-clicking it and then pressing Ctrl-C; then paste it into the Program.cs of a Windows Forms project. It should compile and run, displaying the form, but the buttons don't do anything yet.



## A BGW That Updates Progress Frequently



Here's what the code looks like for a BGW that checks in frequently:




using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

partial class MainForm
{
  private BackgroundWorker backgroundWorker;

  partial void startButton_Click(object sender, EventArgs e)
  {
    // Start the background task without error.
    this.StartBackgroundTask(false);

    // Update UI to reflect background task.
    this.TaskIsRunning();
  }

  partial void errorButton_Click(object sender, EventArgs e)
  {
    // Start the background task with error.
    this.StartBackgroundTask(true);

    // Update UI to reflect background task.
    this.TaskIsRunning();
  }

  partial void cancelButton_Click(object sender, EventArgs e)
  {
    // Cancel the background task.
    this.backgroundWorker.CancelAsync();

    // The UI will be updated by the cancellation handler.
  }

  private void StartBackgroundTask(bool causeError)
  {
    this.backgroundWorker = new BackgroundWorker();
    this.backgroundWorker.WorkerReportsProgress = true;
    this.backgroundWorker.WorkerSupportsCancellation = true;
    this.backgroundWorker.DoWork += (_, args) =>
    {
      for (int i = 0; i != 100; ++i)
      {
        // Check for cancellation.
        if (this.backgroundWorker.CancellationPending)
        {
          args.Cancel = true;
          return;
        }

        Thread.Sleep(30); // Do some work.

        // Report progress of the work.
        this.backgroundWorker.ReportProgress(i);
      }

      // After all that work, cause the error if requested.
      if (causeError)
      {
        throw new InvalidOperationException("Oops...");
      }

      // The answer, at last!
      args.Result = 42;
    };
    this.backgroundWorker.ProgressChanged += (_, args) =>
    {
      // Update UI to reflect the progress.
      this.progressBar.Value = args.ProgressPercentage;
    };
    this.backgroundWorker.RunWorkerCompleted += (_, args) =>
    {
      // Update UI to reflect completion.
      this.progressBar.Value = 100;

      // Display results.
      if (args.Error != null)
        MessageBox.Show("Background task error: " + args.Error.ToString());
      else if (args.Cancelled)
        MessageBox.Show("Background task cancelled");
      else
        MessageBox.Show("Background task result: " + args.Result);

      // Reset UI.
      this.TaskIsComplete();
    };

    // Kick off the background task.
    this.backgroundWorker.RunWorkerAsync();
  }
}




You can copy and paste this code into a cs file in the Windows Forms solution, such as MainForm.cs. The solution should then build, and you can play with the buttons to test all three scenarios (successful completion, error condition, and cancellation).



## A Task That Updates Progress Frequently



Using the ProgressReporter class (defined below), translating this BGW code to Task code is rather easy; no explicit continuation scheduling is needed:




using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

partial class MainForm
{
  private CancellationTokenSource cancellationTokenSource;

  partial void startButton_Click(object sender, EventArgs e)
  {
    // Start the background task without error.
    this.StartBackgroundTask(false);

    // Update UI to reflect background task.
    this.TaskIsRunning();
  }

  partial void errorButton_Click(object sender, EventArgs e)
  {
    // Start the background task with error.
    this.StartBackgroundTask(true);

    // Update UI to reflect background task.
    this.TaskIsRunning();
  }

  partial void cancelButton_Click(object sender, EventArgs e)
  {
    // Cancel the background task.
    this.cancellationTokenSource.Cancel();

    // The UI will be updated by the cancellation handler.
  }

  private void StartBackgroundTask(bool causeError)
  {
    this.cancellationTokenSource = new CancellationTokenSource();
    var cancellationToken = this.cancellationTokenSource.Token;
    var progressReporter = new ProgressReporter();
    var task = Task.Factory.StartNew(() =>
    {
      for (int i = 0; i != 100; ++i)
      {
        // Check for cancellation 
        cancellationToken.ThrowIfCancellationRequested();

        Thread.Sleep(30); // Do some work. 

        // Report progress of the work. 
        progressReporter.ReportProgress(() =>
        {
          // Note: code passed to "ReportProgress" can access UI elements freely. 
          this.progressBar.Value = i;
        });
      }

      // After all that work, cause the error if requested.
      if (causeError)
      {
        throw new InvalidOperationException("Oops...");
      }

      // The answer, at last! 
      return 42;
    }, cancellationToken);

    // ProgressReporter can be used to report successful completion,
    //  cancelation, or failure to the UI thread. 
    progressReporter.RegisterContinuation(task, () =>
    {
      // Update UI to reflect completion.
      this.progressBar.Value = 100;

      // Display results.
      if (task.Exception != null)
        MessageBox.Show("Background task error: " + task.Exception.ToString());
      else if (task.IsCanceled)
        MessageBox.Show("Background task cancelled");
      else
        MessageBox.Show("Background task result: " + task.Result);

      // Reset UI.
      this.TaskIsComplete();
    });
  }
}




You can copy and paste this code into a cs file in the Windows Forms solution, such as MainForm.cs. The solution won't build until you add the code for the ProgressReporter class below.



## The ProgressReporter Class



The ProgressReporter class is responsible for two things: the reporting of _progress_ by a background task, and the reporting of a _final result_ by the background task.





A background Task calls ProgressReporter.ReportProgress to report progress to the UI thread. This method will pause the background task until the UI has finished updating; if the task does not need to wait, then it can call ProgressReporter.ReportProgressAsync.





The code starting the background Task can also use ProgressReporter to retrieve the final result of the background task. This is done by calling the ProgressReporter.RegisterContinuation method. The delegate passed to this method is executed in the UI thread context after the background task completes. The delegate can then examine the Task object for its status (see the example code above).





In addition to the RegisterContinuation method, the ProgressReporter provides RegisterSucceededHandler, RegisterFaultedHandler, and RegisterCancelledHandler methods if it is easier to handle these situations separately.





The code for this class is not very complex:




using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary> 
/// A class used by Tasks to report progress or completion updates back to the UI. 
/// </summary> 
public sealed class ProgressReporter
{
  /// <summary> 
  /// The underlying scheduler for the UI's synchronization context. 
  /// </summary> 
  private readonly TaskScheduler scheduler;

  /// <summary> 
  /// Initializes a new instance of the <see cref="ProgressReporter"/> class.
  /// This should be run on a UI thread. 
  /// </summary> 
  public ProgressReporter()
  {
    this.scheduler = TaskScheduler.FromCurrentSynchronizationContext();
  }

  /// <summary> 
  /// Gets the task scheduler which executes tasks on the UI thread. 
  /// </summary> 
  public TaskScheduler Scheduler
  {
    get { return this.scheduler; }
  }

  /// <summary> 
  /// Reports the progress to the UI thread. This method should be called from the task.
  /// Note that the progress update is asynchronous with respect to the reporting Task.
  /// For a synchronous progress update, wait on the returned <see cref="Task"/>. 
  /// </summary> 
  /// <param name="action">The action to perform in the context of the UI thread.
  /// Note that this action is run asynchronously on the UI thread.</param> 
  /// <returns>The task queued to the UI thread.</returns> 
  public Task ReportProgressAsync(Action action)
  {
    return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, this.scheduler);
  }

  /// <summary> 
  /// Reports the progress to the UI thread, and waits for the UI thread to process
  /// the update before returning. This method should be called from the task. 
  /// </summary> 
  /// <param name="action">The action to perform in the context of the UI thread.</param> 
  public void ReportProgress(Action action)
  {
    this.ReportProgressAsync(action).Wait();
  }

  /// <summary> 
  /// Registers a UI thread handler for when the specified task finishes execution,
  /// whether it finishes with success, failiure, or cancellation. 
  /// </summary> 
  /// <param name="task">The task to monitor for completion.</param> 
  /// <param name="action">The action to take when the task has completed, in the context of the UI thread.</param> 
  /// <returns>The continuation created to handle completion. This is normally ignored.</returns> 
  public Task RegisterContinuation(Task task, Action action)
  {
    return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.None, this.scheduler);
  }

  /// <summary> 
  /// Registers a UI thread handler for when the specified task finishes execution,
  /// whether it finishes with success, failiure, or cancellation. 
  /// </summary> 
  /// <typeparam name="TResult">The type of the task result.</typeparam> 
  /// <param name="task">The task to monitor for completion.</param> 
  /// <param name="action">The action to take when the task has completed, in the context of the UI thread.</param> 
  /// <returns>The continuation created to handle completion. This is normally ignored.</returns> 
  public Task RegisterContinuation<TResult>(Task<TResult> task, Action action)
  {
    return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.None, this.scheduler);
  }

  /// <summary> 
  /// Registers a UI thread handler for when the specified task successfully finishes execution. 
  /// </summary> 
  /// <param name="task">The task to monitor for successful completion.</param> 
  /// <param name="action">The action to take when the task has successfully completed, in the context of the UI thread.</param> 
  /// <returns>The continuation created to handle successful completion. This is normally ignored.</returns> 
  public Task RegisterSucceededHandler(Task task, Action action)
  {
    return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, this.scheduler);
  }

  /// <summary> 
  /// Registers a UI thread handler for when the specified task successfully finishes execution
  /// and returns a result. 
  /// </summary> 
  /// <typeparam name="TResult">The type of the task result.</typeparam> 
  /// <param name="task">The task to monitor for successful completion.</param> 
  /// <param name="action">The action to take when the task has successfully completed, in the context of the UI thread.
  /// The argument to the action is the return value of the task.</param> 
  /// <returns>The continuation created to handle successful completion. This is normally ignored.</returns> 
  public Task RegisterSucceededHandler<TResult>(Task<TResult> task, Action<TResult> action)
  {
    return task.ContinueWith(t => action(t.Result), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, this.Scheduler);
  }

  /// <summary> 
  /// Registers a UI thread handler for when the specified task becomes faulted. 
  /// </summary> 
  /// <param name="task">The task to monitor for faulting.</param> 
  /// <param name="action">The action to take when the task has faulted, in the context of the UI thread.</param> 
  /// <returns>The continuation created to handle faulting. This is normally ignored.</returns> 
  public Task RegisterFaultedHandler(Task task, Action<Exception> action)
  {
    return task.ContinueWith(t => action(t.Exception), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, this.Scheduler);
  }

  /// <summary> 
  /// Registers a UI thread handler for when the specified task becomes faulted. 
  /// </summary> 
  /// <typeparam name="TResult">The type of the task result.</typeparam> 
  /// <param name="task">The task to monitor for faulting.</param> 
  /// <param name="action">The action to take when the task has faulted, in the context of the UI thread.</param> 
  /// <returns>The continuation created to handle faulting. This is normally ignored.</returns> 
  public Task RegisterFaultedHandler<TResult>(Task<TResult> task, Action<Exception> action)
  {
    return task.ContinueWith(t => action(t.Exception), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, this.Scheduler);
  }

  /// <summary> 
  /// Registers a UI thread handler for when the specified task is cancelled. 
  /// </summary> 
  /// <param name="task">The task to monitor for cancellation.</param> 
  /// <param name="action">The action to take when the task is cancelled, in the context of the UI thread.</param> 
  /// <returns>The continuation created to handle cancellation. This is normally ignored.</returns> 
  public Task RegisterCancelledHandler(Task task, Action action)
  {
    return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled, this.Scheduler);
  }

  /// <summary> 
  /// Registers a UI thread handler for when the specified task is cancelled. 
  /// </summary> 
  /// <typeparam name="TResult">The type of the task result.</typeparam> 
  /// <param name="task">The task to monitor for cancellation.</param> 
  /// <param name="action">The action to take when the task is cancelled, in the context of the UI thread.</param> 
  /// <returns>The continuation created to handle cancellation. This is normally ignored.</returns> 
  public Task RegisterCancelledHandler<TResult>(Task<TResult> task, Action action)
  {
    return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled, this.Scheduler);
  }
}




You can copy and paste this code into a cs file in the Windows Forms solution, such as ProgressReporter.cs. The solution should then build, and you can play with the buttons to test all three scenarios (successful completion, error condition, and cancellation).

