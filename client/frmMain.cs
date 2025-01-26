using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using CTrue.FsConnect;

namespace VirtualFlightOnlineTransmitter
{
	public partial class frmMain : Form
	{
		/// <summary>Event handler to capture data received from SimConnect</summary>
		/// <param name="latitude">Aircraft Latitude</param>
		/// <param name="longitude">Aircraft Longitude</param>
		/// <param name="alititude">Aircraft Altitude</param>
		/// <param name="heading">Aircraft Heading</param>
		/// <param name="airspeed">Aircraft Airspeed</param>
		/// <param name="groundspeed">Aircraft Groundspeed</param>
		public delegate void DataReceivedEventHandler(string aircraft_type, double latitude, double longitude, double alititude,
																									double heading, double airspeed, double groundspeed, double touchdown_velocity,
																									string transponder_code);
		public event DataReceivedEventHandler DataReceivedEvent;

		/// <summary>FSConnect library to communicate with the flight simulator</summary>
		private FsConnect flightSimulatorConnection;

		/// <summary>Control variables used to manage connections with FSConnect</summary>
		public int planeInfoDefinitionId { get; set; }

		public enum Requests
		{
			PlaneInfoRequest = 0
		}

		public const string Id = "as49d216-e00f4-4a63-b73c-f62c1144b54242";
		public const string Application_Title = "VFO Transmitter+ 1.0.1.20" + AssemblyVersionAttribute;

		private bool fakeFsConnectForDebug = false;
		private bool fakeFsConnected = false;

		private bool dataReceived = false;
		private bool dataSendOkay = false;

		private static int TICK_MILLIS = 200; // check State every 100ms
		private int count = 0;

		/// <summary>Constructor for the Form</summary>
		public frmMain(bool debug)
		{
			fakeFsConnectForDebug = debug;

			state = State.Off;
			InitializeComponent();
			// Disable illegal cross thread calls warnings
			Control.CheckForIllegalCrossThreadCalls = false;
			// Attach an event reveiver to the data received event
			this.DataReceivedEvent += HandleDataReceived;
		}

		/// <summary>Event loading the form reads Properties </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Main_Load(object sender, EventArgs e)
		{

			// pre-fill the settings boxes with data from properties
			tbServerURL.Text = Properties.Settings.Default.ServerURL;
			tbPin.Text = Properties.Settings.Default.Pin;
			cbMSFSServer.Text = Properties.Settings.Default.MSFSServer;
			tbCallsign.Text = Properties.Settings.Default.Callsign;
			tbPilotName.Text = Properties.Settings.Default.PilotName;
			tbGroupName.Text = Properties.Settings.Default.GroupName;
			tbNotes.Text = Properties.Settings.Default.Notes;
			tbRefresh.Text = Properties.Settings.Default.RefreshMillis.ToString();

			tmrTransmit.Start(); // transmits data every few seconds
		}
		// ------------------------------------------------------------------------------------
		/// <summary>State machine</summary>
		enum State
		{
			Off, On, Connecting, Connected, Receiving, Waiting, Broken
		}
		private State state = State.Off;

		/// <summary>Stateful tick events management</summary>
		//
		// OFF - ON - Connecting - Connected - Receiving --> Waiting ( -> Connected )
		// Failure: Waiting( -> ON )
		void OnTick(object sender, EventArgs e)
		{
			switch (state)
			{
				case State.Off: SetMessage("Off"); break;

				case State.On: Connect(); break;
				case State.Connecting: AwaitResponse(); break; // wait to be connected
				case State.Broken: Waiting(State.On); break;

				case State.Connected: RequestFlightData(); break;
				case State.Receiving: Receiving(); break;
				case State.Waiting: Waiting(State.Connected); break;
			}
		}

		/// <summary>Wait until time is ready to request flight position</summary>
		private void RequestFlightData()
		{
			count = 0;
			dataReceived = false;
			try
			{
				if (fakeFsConnectForDebug && fakeFsConnected)
				{
					state = State.Receiving;
				}
				else
				{
					if (flightSimulatorConnection.Connected)
					{
						state = State.Receiving;
						flightSimulatorConnection.RequestData((int)Requests.PlaneInfoRequest, this.planeInfoDefinitionId);
						SetMessage("Data ...");
					}
					else if (flightSimulatorConnection.Paused)
					{
						count = 0;
						state = State.Waiting;
						SetMessage("Sim paused");
					}
					else
					{
						count = 0;
						state = State.Broken;
						SetMessage("Sim gone?");
					}
				}
			}
			catch (Exception ex)
			{
				count = 0;
				state = State.Broken;
				SetMessage("Data error");
			}
		}

		private void Receiving()
		{
			if (fakeFsConnectForDebug && fakeFsConnected)
				FakeFlightData();
		}

		private void FakeFlightData()
		{
			dataReceived = true;
			Random rnd = new Random();
			string aircraft_type = "Cessna";
			double latitude = 53.0 + rnd.NextDouble() / 10.0;
			double longitude = 10.0 + rnd.NextDouble() / 10.0;
			double altitude = 200.0 + rnd.NextDouble() * 20.0;
			double heading = 90.0 + rnd.NextDouble() * 5.0;
			double airspeed = 100.0 + rnd.NextDouble() * 20.0;
			double groundspeed = 160.0;
			double touchdown_velocity = 1.0;
			string transponder_code = "42";

			this.SendDataToServer(aircraft_type, latitude, longitude, heading, altitude,
															airspeed, groundspeed, touchdown_velocity, transponder_code.ToString());
			this.DataReceivedEvent(aircraft_type, latitude, longitude, altitude, heading, airspeed,
															groundspeed, touchdown_velocity, transponder_code);
			if (state != State.Broken)
				state = State.Waiting;
		}


		private void Waiting(State nextState)
		{
			count++;
			if (count > Properties.Settings.Default.RefreshMillis / TICK_MILLIS)
			{
				state = nextState;
				count = 0;
			}
		}

		/// <summary>Handler to receive information from SimConnect</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void HandleReceivedFsData(object sender, FsDataReceivedEventArgs e)
		{
			try
			{
				if (e.RequestId == (uint)Requests.PlaneInfoRequest)
				{
					PlaneInfoResponse r = (PlaneInfoResponse)e.Data.FirstOrDefault();
					string aircraft_type = r.Title;
					double latitude = r.PlaneLatitude;
					double longitude = r.PlaneLongitude;
					double altitude = r.IndicatedAltitude;
					double heading = r.PlaneHeadingDegreesTrue;
					double airspeed = r.AirspeedIndicated;
					double groundspeed = r.GpsGroundSpeed;
					double touchdown_velocity = r.PlaneTouchdownNormalVelocity;

					dataReceived = true;
					SetMessage("Data ok");

					// TODO - look into why TransponderCode doesn't come back from the simvars
					String transponder_code = Bcd.Bcd2Dec(r.TransponderCode).ToString();
					this.DataReceivedEvent(aircraft_type, latitude, longitude, altitude, heading, airspeed,
																	groundspeed, touchdown_velocity, transponder_code);
					this.SendDataToServer(aircraft_type, latitude, longitude, heading, altitude,
																 airspeed, groundspeed, touchdown_velocity, transponder_code);
				}
				else
				{
					SetMessage("FS " + e.RequestId);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				SetMessage("Error");
				state = State.Broken;
			}
		}

		/// <summary>Event Handler to handle data returning from the simulator</summary>
		/// <param name="latitude"></param>
		/// <param name="longitude"></param>
		/// <param name="altitude"></param>
		/// <param name="heading"></param>
		/// <param name="airspeed"></param>
		public void HandleDataReceived(string aircraft_type, double latitude, double longitude, double altitude, double heading, double airspeed, double groundspeed, double touchdown_velocity, string transponder_code)
		{
			// convert ground speed from metres per second to knots
			groundspeed = groundspeed * 1.94384449;

			// Update the Screen   
			this.tbAircraftType.Text = aircraft_type;
			this.tbLatitude.Text = LatitudeToString(latitude);
			this.tbLongitude.Text = LongitudeToString(longitude);
			this.tbAltitude.Text = string.Format("{0:0. ft}", altitude);
			this.tbHeading.Text = string.Format("{0:0. deg}", heading);
			this.tbAirspeed.Text = string.Format("{0:0. knots}", airspeed);
			this.tbGroundspeed.Text = string.Format("{0:0. knots}", groundspeed);
			this.tbTouchdownVel.Text = string.Format("{0:0. ft/min}", touchdown_velocity * 60);
			this.Refresh();
		}

		/// <summary>Sends aircraft data to the web server</summary>
		/// <param name="latitude" >Aircraft Latitude</param>
		/// <param name="longitude">Aircraft Longitude</param>
		/// <param name="heading"  >Aircraft Heading</param>
		/// <param name="altitude" >Aircraft Altitude</param>
		/// <param name="airspeed" >Aircraft Airspeed</param>
		/// <returns>Response from GET request to Server</returns>      
		public string SendDataToServer(string aircraft_type, double latitude, double longitude,
																	 double heading, double altitude, double airspeed, double groundspeed,
																	 double touchdown_velocity, string transponder_code)
		{
			string result = "";
			string notes = Properties.Settings.Default.Notes;
			string version = System.Windows.Forms.Application.ProductVersion;

			try
			{
				dataSendOkay = false;
				SetMessage("Sending...");
				// force the numbers into USA format
				CultureInfo usa_format = new CultureInfo("en-US");
				string fixedData =
							"Callsign=" + Properties.Settings.Default.Callsign
						+ "&PilotName=" + Properties.Settings.Default.PilotName
						+ "&GroupName=" + Properties.Settings.Default.GroupName
						+ "&MSFSServer=" + Properties.Settings.Default.MSFSServer
						+ "&Pin=" + Properties.Settings.Default.Pin
						+ "&Notes=" + WebUtility.UrlEncode(notes)
						+ "&Version=" + version;

				string url = Properties.Settings.Default.ServerURL + "?"
						+ fixedData
						// dynamic data from MSFS
						+ "&AircraftType=" + aircraft_type.ToString()
						+ "&Latitude=" + latitude.ToString(usa_format)
						+ "&Longitude=" + longitude.ToString(usa_format)
						+ "&Altitude=" + altitude.ToString(usa_format)
						+ "&Airspeed=" + airspeed.ToString(usa_format)
						+ "&Groundspeed=" + groundspeed.ToString(usa_format)
						+ "&Heading=" + heading.ToString(usa_format)
						+ "&TouchdownVelocity=" + touchdown_velocity.ToString(usa_format)
						+ "&TransponderCode=" + transponder_code;

				var request = WebRequest.Create(url);

				request.Method = "GET";
				request.Timeout = 1000; // 1 second
				using (var webResponse = request.GetResponse())
				{
					using (var webStream = webResponse.GetResponseStream())
					{
						using (var reader = new StreamReader(webStream))
						{
							result = reader.ReadToEnd();
							dataSendOkay = true;
							SetMessage("Data send");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				SetMessage("SERVER Down");
			}
			return result;
		}

		void GoOffline(string message)
		{
			EnableTextBoxes(true);
			state = State.Off;
			dataReceived = false;
			dataSendOkay = false;
			SetMessage(message);
		}

		private bool IsFsConnected()
		{
			return (flightSimulatorConnection != null && flightSimulatorConnection.Connected)
				|| fakeFsConnected;
		}

		/// <summary>Helper method to connect to SimConnect and update the interface appropriately</summary>
		private void Connect()
		{
			if (state != State.On || state == State.Connected)
				return;

			state = State.Connecting;
			count = 0;

			// first check if the default parameters have been changed, check if the parameters are empty
			if (tbCallsign.Text == "Your Callsign" || tbCallsign.Text == string.Empty ||
					tbPilotName.Text == "Your Name" || tbPilotName.Text == string.Empty ||
					tbGroupName.Text == string.Empty || tbServerURL.Text == string.Empty ||
					Properties.Settings.Default.RefreshMillis < 500)
			{
				MessageBox.Show("It looks like you haven't changed your server, callsign, name or groupname yet.\n " +
												"Please set them properly before connecting.",
												"Let's do this first", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				GoOffline("Off - Please change your data");
				return;
			}
			// Disable the textboxes
			EnableTextBoxes(false);
			SetMessage("Sim Connecting...");

			this.ConnectionStartTime = DateTime.Now;
			if (fakeFsConnectForDebug)
			{
				fakeFsConnected = true;
				SetMessage("Sim Faking...");
			}
			else
			{
				try
				{
					// connect to simulator, and lazy loading
					if (flightSimulatorConnection == null)
					{
						this.flightSimulatorConnection = new FsConnect();
					}
					this.flightSimulatorConnection.Connect("VirtualFlightOnlineClient");
					this.planeInfoDefinitionId = this.flightSimulatorConnection.RegisterDataDefinition<PlaneInfoResponse>();

					// Attach event handler
					this.flightSimulatorConnection.FsDataReceived += this.HandleReceivedFsData;

					SetMessage(" ");
					state = State.Connected;
				}
				catch (Exception ex)
				{
					SetMessage("Sim failed");
					state = State.Broken;
					count = 0;
				}
			}
		}

		private void AwaitResponse()
		{
			if (fakeFsConnectForDebug && fakeFsConnected)
			{
				state = State.Connected;
				fakeFsConnected = true;
			}
		}


		/// <summary>Helper method to disconnect from SimConnect and update the interface appropriately</summary>
		private void Disconnect()
		{
			dataReceived = false;
			dataSendOkay = false;
			if (state != State.Off) state = State.On;
			SetMessage(" ");

			try
			{
				if (fakeFsConnectForDebug)
					fakeFsConnected = false;
				else if (flightSimulatorConnection.Connected || flightSimulatorConnection.Paused)
				{
					flightSimulatorConnection.FsDataReceived -= this.HandleReceivedFsData;
					flightSimulatorConnection.Disconnect();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			// switch the UI components back on
			EnableTextBoxes(true);
		}

		void SetMessage(string message)
		{
			string text = "Simu: " + (IsFsConnected() ? "ON" : "--") +
									" | Data: " + (dataReceived ? "OK" : "--") +
									" | Serv: " + (dataSendOkay ? "OK" : "--") +
									" | " + message;
			text = text.Substring(0, Math.Min(text.Length, 49));
			tsslMain.Text = text;
			this.Refresh();
		}

		private void EnableTextBoxes(bool enabled)
		{
			resetSettingsToDefaultsToolStripMenuItem.Enabled = !enabled;

			tbServerURL.Enabled = enabled;
			tbPin.Enabled = enabled;
			cbMSFSServer.Enabled = enabled;
			tbCallsign.Enabled = enabled;
			tbPilotName.Enabled = enabled;
			tbGroupName.Enabled = enabled;
			tbNotes.Enabled = enabled;
			tbRefresh.Enabled = enabled;

			btnConnect.Enabled = enabled;
			btnDisconnect.Enabled = !enabled;
			this.Refresh();
		}

		/// <summary>format decimal longitude to -> x°m's" E | W</summary>
		/// <param name="dec"></param>
		/// <returns></returns>
		string LongitudeToString(double val)
		{
			return val2coords(val) + " " + (val > 0 ? "E" : "W");
		}

		/// <summary>Format decimal latitude to ->  y°m's" N | S</summary>
		/// <param name="val"></param>
		/// <returns></returns>
		string LatitudeToString(double val)
		{
			return val2coords(val) + " " + (val > 0 ? "N" : "S");
		}

		/// <summary>Format decimal to ->  d°m's" </summary>
		private string val2coords(double val)
		{
			int d = (int)val;
			int m = (int)((val - d) * 60);
			double s = ((((val - d) * 60) - m) * 60);
			return Math.Abs(d) + "° " + Math.Abs(m) + "' " + string.Format("{0:0.00}", Math.Abs(s)) + "\"";
		}

		private void btnConnect_Click(object sender, EventArgs e)
		{
			state = State.On;
		}

		private void btnDisconnect_Click(object sender, EventArgs e)
		{
			Disconnect();
			state = State.Off;
		}

		/// <summary>last actions on closing of the form</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			// If the simulator is connected, ask the user if they really want to close Transmitter
			if (IsFsConnected())
			{
				DialogResult result = MessageBox.Show("Transmitter is still connected to the simulator - are you sure you want to close it?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				if (result == DialogResult.Yes)
				{
					// disconnect from the simulator
					this.Disconnect();
				}
				else
				{  // cancel the form closure
					e.Cancel = true;
				}
			}
		}

		private void tbServerURL_TextChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.ServerURL = tbServerURL.Text;
			Properties.Settings.Default.Save();
		}
		private void tbPin_TextChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.Pin = tbPin.Text;
			Properties.Settings.Default.Save();
		}
		private void tbCallsign_TextChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.Callsign = tbCallsign.Text;
			Properties.Settings.Default.Save();
		}
		private void tbPilotName_TextChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.PilotName = tbPilotName.Text;
			Properties.Settings.Default.Save();
		}
		private void tbGroupName_TextChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.GroupName = tbGroupName.Text;
			Properties.Settings.Default.Save();
		}
		private void cbMSFSServer_SelectedIndexChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.MSFSServer = cbMSFSServer.Text;
			Properties.Settings.Default.Save();
		}
		private void tbNotes_TextChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.Notes = tbNotes.Text;
			Properties.Settings.Default.Save();
		}

		private void tbRefresh_TextChanged(object sender, EventArgs e)
		{
			int value = 5000;
			try
			{
				value = int.Parse(tbRefresh.Text);
			}
			catch (FormatException formatException)
			{
				SetMessage(formatException.Message);
			}
			if (value < 500)
			{
				value = 500;
				tbRefresh.Text = "500";
			}
			Properties.Settings.Default.RefreshMillis = value;
			Properties.Settings.Default.Save();
		}

		private void tbRefresh_Leave(object sender, EventArgs e)
		{
			tbRefresh.Text = tbRefresh.Text.Trim();
		}
		private void tbServerURL_Leave(object sender, EventArgs e)
		{
			tbServerURL.Text = tbServerURL.Text.Trim();
		}
		private void tbPin_Leave(object sender, EventArgs e)
		{
			tbPin.Text = tbPin.Text.Trim();
		}
		private void tbCallsign_Leave(object sender, EventArgs e)
		{
			tbCallsign.Text = tbCallsign.Text.Trim();
		}
		private void tbPilotName_Leave(object sender, EventArgs e)
		{
			tbPilotName.Text = tbPilotName.Text.Trim();
		}
		private void tbGroupName_Leave(object sender, EventArgs e)
		{
			tbGroupName.Text = tbGroupName.Text.Trim();
		}
		private void tbNotes_Leave(object sender, EventArgs e)
		{
			tbNotes.Text = tbNotes.Text.Trim();
		}


		/// <summary>Handle users clicking on the About menu option (show a message)</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string version = System.Windows.Forms.Application.ProductVersion;
			MessageBox.Show("Transmitter\nby Jonathan Beckett\nVirtual Flight Online\nhttps://virtualflight.online\nVersion " + version, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		/// <summary>Exits the application (shuts things down first)</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			Disconnect();
			state = State.Off;
			// close the application
			this.Close();
		}


		/// <summary>Resets the textboxes</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void resetSettingsToDefaultsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!this.flightSimulatorConnection.Connected)
			{
				tbServerURL.Text = "https://yourserver/send.php";
				tbPin.Text = "1234";
				tbCallsign.Text = "Your Callsign";
				tbPilotName.Text = "Your Name";
				tbGroupName.Text = "Your Group";
				cbMSFSServer.Text = "SOUTH EAST ASIA";
				tbRefresh.Text = "5000";

				// save the settings
				Properties.Settings.Default.Save();
			}
			else
			{
				MessageBox.Show("Please disconnect from the simulator first", "Disconnect First", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void aircraftDataToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (aircraftDataToolStripMenuItem.Checked)
			{
				this.Height = this.MaximumSize.Height;
			}
			else
			{
				this.Height = this.MinimumSize.Height;
			}
		}

		/// <summary>Time the connection to the simulator started (used to calculate how long the user has been connected)</summary>
		public DateTime ConnectionStartTime { get; set; }

		/// <summary>Data structure used to receive information from SimConnect</summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
		public struct PlaneInfoResponse
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public String Title;
			[SimVar(NameId = FsSimVar.PlaneLatitude, UnitId = FsUnit.Degree)]
			public double PlaneLatitude;
			[SimVar(NameId = FsSimVar.PlaneLongitude, UnitId = FsUnit.Degree)]
			public double PlaneLongitude;
			[SimVar(NameId = FsSimVar.IndicatedAltitude, UnitId = FsUnit.Feet)]
			public double IndicatedAltitude;
			[SimVar(NameId = FsSimVar.GpsPositionAlt, UnitId = FsUnit.Meter)]
			public double GpsPositionAlt;
			[SimVar(NameId = FsSimVar.PlaneAltitude, UnitId = FsUnit.Feet)]
			public double PlaneAltitude;
			[SimVar(NameId = FsSimVar.PlaneHeadingDegreesTrue, UnitId = FsUnit.Degree)]
			public double PlaneHeadingDegreesTrue;
			[SimVar(NameId = FsSimVar.PlaneHeadingDegreesMagnetic, UnitId = FsUnit.Degree)]
			public double PlaneHeadingDegreesMagnetic;
			[SimVar(NameId = FsSimVar.AirspeedIndicated, UnitId = FsUnit.Knot)]
			public double AirspeedIndicated;
			[SimVar(NameId = FsSimVar.GpsGroundSpeed, UnitId = FsUnit.MetersPerSecond)]
			public double GpsGroundSpeed;
			[SimVar(NameId = FsSimVar.PlaneTouchdownNormalVelocity, UnitId = FsUnit.FeetPerSecond)]
			public double PlaneTouchdownNormalVelocity;

			// TODO - look into why TransponderCode doesn't come back from the simvars :)
			[SimVar(NameId = FsSimVar.TransponderCode, UnitId = FsUnit.Bco16)]
			public uint TransponderCode;
		}
	}
}
