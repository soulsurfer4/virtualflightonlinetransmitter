using System;
using System.Threading;
using System.Windows.Forms;

namespace VirtualFlightOnlineTransmitter
{
	/// <summary>
	/// Helper class that encapsulates the execution of an action only once at the first call.
	/// Subsequent calls of the same action will be ignored, as long as the first action call has not finished.
	/// </summary>
	public static class SingleInstance
	{
		/// <summary>
		/// Execute the first action once or execute the alternative action when the first is not yet finished.
		/// </summary>
		public static void Start(string uniqueApplicationDescription, string applicationTitle, Action oneInstanceAction, Action instanceNotUniqueAction = null)
		{
			bool isFirstInstance = true;
			Semaphore mutex = null;
			try
			{
				// if there is no custom second action, set a default one.
				if (instanceNotUniqueAction == null)
				{
					instanceNotUniqueAction = () => MessageBox.Show("Application is already running!", applicationTitle,
							MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				// 
				mutex = new Semaphore(0, 1, uniqueApplicationDescription, out isFirstInstance);
				if (isFirstInstance)
				{
					oneInstanceAction();
				}
				else
				{
					instanceNotUniqueAction();
				}
			}
			finally
			{
				if (isFirstInstance)
				{
					if (mutex != null)
					{
						mutex.Close();
					}
				}
			}
		}
	}
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			string debug = "";
			if (args.Length > 0)
			{
				debug = args[0];
			}
			SingleInstance.Start(frmMain.Id, frmMain.Application_Title, () =>
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				Application.Run(new frmMain(debug));
			}
			);
		}
	}

}
