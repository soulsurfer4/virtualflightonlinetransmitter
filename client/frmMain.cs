using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using CTrue.FsConnect;
using static System.Net.WebRequestMethods;

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

		/// <summary>FSConnect library to communicate with the flight simulator</summary>
		private FsConnect fsConnector;

		/// <summary>Control variables used to manage connections with FSConnect</summary>
		public int planeInfoDefinitionId { get; set; }

		public enum Requests
		{
			PlaneInfoRequest = 0
		}


		public const string Id = "as49d216-e00f4-4a63-b73c-f62c1144b54242";
		public const string Application_Title = "VirtualFlightTransmitter - ";

		private string PropServerURL;
		private string PropPin;
		private int    PropRefreshMillis;
		private string PropMSFSServer;
		private string PropCallsign;
		private string PropPilotName;
		private string PropGroupName;
		private string PropNotes;
		private string PropMagicArgs;

		private bool filelog = false;
		private bool loglite = false;
		private bool autoOn = false;
		private bool autoKill = false;
		private bool autoKillActivated = false;

		private string simVersion   = null;
		private string lastSimState = null;
		private bool   simPaused    = false;
		private bool   simConnected = false;
		private bool dataReceived   = false;
		private bool dataSendOkay   = false;

		private static int REFRESH_MILLIS_MIN = 2000;
		private static int TICK_MILLIS = 200; // check State every 200ms
		private static long timeToSendAgain = 0;
		private int count = 0;

		/// <summary>Constructor for the Form</summary>
		public frmMain(string[] args)
		{
			PropLoad();
			if (PropPilotName == null)
			{
				ResetProps();
			}
			checkArgs(args);

			state = State.Off;
			InitializeComponent();
			// Disable illegal cross thread calls warnings
			Control.CheckForIllegalCrossThreadCalls = false;

		}

		/// <summary>Event loading the form reads Properties </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Main_Load(object sender, EventArgs e)
		{
			// pre-fill the settings boxes with data from properties
			checkArgs(PropMagicArgs.Split(','));

			setPropsToDisplay();
			LogInfo("Options: log " + filelog + ", autoOn " + autoOn + ", autoKill " + autoKill + ", loglite " + loglite);

			tmrTransmit.Start(); // transmits data every few seconds
			if (autoOn)
				UserConnect();
		}

		private void setPropsToDisplay()
		{
			tbServerURL.Text = PropServerURL;
			tbPin.Text = PropPin;
			tbRefresh.Text = PropRefreshMillis.ToString();
			cbMSFSServer.Text = PropMSFSServer;
			tbCallsign.Text = PropCallsign;
			tbPilotName.Text = PropPilotName;
			tbGroupName.Text = PropGroupName;
			tbNotes.Text = PropNotes;

		}
		private void checkArgs(string[] args)
		{
			foreach (string arg in args)
			{
				if (arg == null || arg.Length == 0)
					continue;
				
				//WriteLine("Startparameter: " + arg);
				filelog  = filelog  || (arg == "log");
				loglite  = loglite  || (arg == "loglite");
				autoOn   = autoOn   || (arg == "ichbinzufauldenconnectbuttonzudrücken");
				autoKill = autoKill || (arg == "ichbinzufauldentransmitterzuschließen");
			}
		}
		// ------------------------------------------------------------------------------------
		/// <summary>State machine</summary>
		enum State
		{
			Off, On, Connecting, Connected, Receiving, Waiting, Breaking, Broken
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

				case State.On        : Connect(); break;
				case State.Connecting: Waiting(State.Breaking); break; // wait to be connected
				case State.Breaking  : Breaking(); break;
				case State.Broken    : Waiting(State.On); break;

				case State.Connected: RequestFlightData(); break;
				case State.Receiving: Waiting(State.Breaking); break;

				case State.Waiting  : Waiting(State.Connected); break;
			}
		}

		/// <summary>Wait until time is ready to request flight position</summary>
		private void RequestFlightData()
		{
			count = 0;
			try
			{
				checkSimStateChange();
				dataReceived = false;
				state = State.Receiving;
				if (fsConnector.Connected)
				{
					if (fsConnector.Paused)
					{
						SetMessage("Sim paused");
						LogDebug("Sim is paused, requesting flight data anyway");
					}
					else
					{
						SetMessage("Data ...");
						LogDebug("requesting flight data");
					}
					fsConnector.RequestData((int)Requests.PlaneInfoRequest, this.planeInfoDefinitionId);
				}
				else
				{
					// 07.02.2025 19:18:14: RequestFlightData, connected: False, paused: False
					state = State.Breaking;
					// 07.02.2025 19:19:15: Disconnected from Sim
					SetMessage("Sim gone?");
					LogInfo("Sim gone?" );
				}
			}
			catch (Exception ex)
			{
				LogDebug("RequestFlightData failed: " + ex.Message);
				count = 0;
				state = State.Breaking;
				SetMessage("Data error");
			}
		}

		private void Waiting(State nextState)
		{
			count++;
			if (TimeIsUp()) { 
				LogDebug("Waiting done: " + state + " -> " + nextState);
				state = nextState;
				checkSimStateChange();
				count = 0;
			}
		}

		private Boolean TimeIsUp()
		{
			return count > PropRefreshMillis / TICK_MILLIS;
		}

		private void Breaking()
		{
			Disconnect();
			// 07.02.2025 19:18:14: RequestFlightData, connected: False, paused: False
			// 07.02.2025 19:19:15: Disconnected from Sim
			state = State.Broken; // hat gefehlt für Neustart
		}

		private void checkSimStateChange()
		{
			if (fsConnector != null)
			{
				string currState = "??";
				if (fsConnector.Paused)
					currState = dataReceived ? "zz" : "xx";
				else if (fsConnector.Connected)
					currState = "ON" ;
				string newVersion = fsConnector.ConnectionInfo.ApplicationVersion;

				if (simVersion != newVersion
						|| simPaused != fsConnector.Paused
						|| simConnected != fsConnector.Connected)
				{
					LogInfo("Sim Version: '" + simVersion + "' => '" + newVersion + "', Sim state " + lastSimState + " => " + currState
						+ ", " + fsConnector.ConnectionInfo.ApplicationName);
					lastSimState = currState;
					simPaused = fsConnector.Paused;
					simConnected = fsConnector.Connected;
					simVersion = newVersion;
					if (newVersion != null && newVersion != "")
						autoKillActivated = true;
				}
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
					state = State.Waiting;
					LogDebug("Received flight data ");
					PlaneInfoResponse  r = (PlaneInfoResponse)e.Data.FirstOrDefault();
					string aircraft_type = r.Title;
					double latitude      = r.PlaneLatitude;
					double longitude     = r.PlaneLongitude;
					double altitude      = r.IndicatedAltitude;
					double heading       = r.PlaneHeadingDegreesTrue;
					double airspeed      = r.AirspeedIndicated;
					double groundspeed   = r.GpsGroundSpeed;
					double touchdown_velocity = r.PlaneTouchdownNormalVelocity;

					dataReceived = true;
					SetMessage("Data ok");
					long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
					if (timestamp < timeToSendAgain)
					{
						LogDebug("Skipped sending received flight data");
					}
					else
					{
						timeToSendAgain = timestamp + PropRefreshMillis;

						// TODO - look into why TransponderCode doesn't come back from the simvars
						String transponder_code = Bcd.Bcd2Dec(r.TransponderCode).ToString();
						// convert ground speed from metres per second to knots
						groundspeed = groundspeed * 1.94384449;

						this.HandleDataReceived(aircraft_type, latitude, longitude, altitude, heading, airspeed,
																		groundspeed, touchdown_velocity, transponder_code);
						this.SendDataToServer(aircraft_type, latitude, longitude, heading, altitude,
																	 airspeed, groundspeed, touchdown_velocity, transponder_code);
					}
				}
				else
				{
					LogDebug("Received unknown Sim data type: " + e.RequestId);
				}
			}
			catch (Exception ex)
			{
				LogDebug("HandleReceivedFsData: " + ex.Message);
				SetMessage("Error");
			}
		}

		/// <summary>Event Handler to handle data returning from the simulator</summary>
		/// <param name="latitude"></param>
		/// <param name="longitude"></param>
		/// <param name="altitude"></param>
		/// <param name="heading"></param>
		/// <param name="airspeed"></param>
		public void HandleDataReceived(string aircraft_type, double latitude, double longitude, double altitude, 
			double heading, double airspeed, double groundspeed, double touchdown_velocity, string transponder_code)
		{
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
			string notes = PropNotes;
			string version = System.Windows.Forms.Application.ProductVersion;
			try
			{
				dataSendOkay = false;
				SetMessage("Sending...");
				// force the numbers into USA format
				CultureInfo usa_format = new CultureInfo("en-US");
				string fixedData =
							"Callsign="    + PropCallsign
						+ "&PilotName="  + PropPilotName
						+ "&GroupName="  + PropGroupName
						+ "&MSFSServer=" + PropMSFSServer
						+ "&Pin="        + PropPin
						+ "&Version="    + version;
				if (notes != null && notes.Trim().Length > 0)
					fixedData += "&Notes=" + WebUtility.UrlEncode(notes);

					string url = PropServerURL + "?"
						+ fixedData
						// dynamic data from MSFS
						+ "&AircraftType=" + aircraft_type.ToString()
						+ "&Latitude="     + latitude.ToString(usa_format)
						+ "&Longitude="    + longitude.ToString(usa_format)
						+ "&Altitude="     + altitude.ToString(usa_format)
						+ "&Airspeed="     + airspeed.ToString(usa_format)
						+ "&Groundspeed="  + groundspeed.ToString(usa_format)
						+ "&Heading="      + heading.ToString(usa_format)
						+ "&TouchdownVelocity=" + touchdown_velocity.ToString(usa_format)
						+ "&TransponderCode="   + transponder_code;

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
							LogDebug("SendDataToServer: " + url.Length + " Bytes");
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogDebug("Server Error response: " + ex.Message);
				SetMessage("SERVER Down");
			}
			return result;
		}

		/// <summary>Helper method to connect to SimConnect and update the interface appropriately</summary>
		private void UserConnect()
		{
			LogInfo("Button Connect");
			Connect();
		}

		private void Connect() {
			dataReceived = false;
			dataSendOkay = false;
			count = 0;
			// Disable the textboxes
			EnableTextBoxes(false);
			SetMessage("Sim Connecting...");
			state = State.Connecting;

			this.ConnectionStartTime = DateTime.Now;
			LogDebug("Connecting to Sim ...");
			try
			{
				// connect to simulator, and lazy loading
				if (fsConnector == null)
				{
					this.fsConnector = new FsConnect();
					LogDebug("Creating new FsConnector");
				}
				fsConnector.Connect("VirtualFlightOnlineClient");
				this.planeInfoDefinitionId = this.fsConnector.RegisterDataDefinition<PlaneInfoResponse>();

				// Attach event handler
				this.fsConnector.FsDataReceived += this.HandleReceivedFsData;

				SetMessage("...");
				checkSimStateChange();
				state = State.Waiting; // State.Connected came too soon, as FsConnector.Connected is "false" yet
			}
			catch (Exception ex)
			{
				checkSimStateChange();
				LogDebug("Connect to Sim failed: " + ex.Message);
				this.fsConnector.Dispose();
				this.fsConnector = null;
				if (autoKill)
				{
					if (autoKillActivated) { 
						LogInfo("AutoKill active - no Sim found - bye bye");
						this.Close();
					} else {
						LogDebug("AutoKill is watching ... ");
					}
				}
				else
				{
					state = State.Broken;
				}
				count = 0;
			}
		}

		private void UserDisconnect()
		{
			LogInfo("Button Disconnect");
			state = State.Off;
			Disconnect();
			// switch the UI components back on
			EnableTextBoxes(true);
		}

		/// <summary>Helper method to disconnect from SimConnect and update the interface appropriately</summary>
		private void Disconnect()
		{
			dataReceived = false;
			dataSendOkay = false;
			try
			{
				if (fsConnector != null)
				{
					fsConnector.Disconnect(); 
					fsConnector.FsDataReceived -= this.HandleReceivedFsData;					
					LogInfo("Breaking: Disconnected from Sim");
				}
			}
			catch (Exception ex)
			{
				LogDebug("Disconnect Error: " + ex.Message);
			}
		}

		void SetMessage(string message)
		{
			string text =   "Sim: " + simVersion + " " + lastSimState  +
									" | Data: " + (dataReceived ? "OK" : "--") +
									" | Serv: " + (dataSendOkay ? "OK" : "--") +
									" | " + message;
			text = text.Substring(0, Math.Min(text.Length, 49));
			tsslMain.Text = text;
			this.Refresh();
		}
		void LogInfo(string line)
		{
			if (loglite) WriteLine(line);
		}
		void LogDebug(string line)
		{
			if (filelog )	WriteLine(line);
		}

			void WriteLine(string line)
		{
			using (StreamWriter w = System.IO.File.AppendText("transmitter.log"))
			{
				w.WriteLine(DateTime.Now + ": " + line);
			}
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
			PropSave();			
			UserConnect();
		}

		private void btnDisconnect_Click(object sender, EventArgs e)
		{
			UserDisconnect();
		}

		/// <summary>last actions on closing of the form</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			LogInfo("Form closing");
			// If the simulator is connected, ask the user if they really want to close Transmitter
			if (simVersion != null && fsConnector != null && fsConnector.Connected)
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
			PropServerURL = tbServerURL.Text;
		}
		private void tbPin_TextChanged(object sender, EventArgs e)
		{
			PropPin = tbPin.Text;
		}
		private void tbCallsign_TextChanged(object sender, EventArgs e)
		{
			PropCallsign = tbCallsign.Text;
		}
		private void tbPilotName_TextChanged(object sender, EventArgs e)
		{
			PropPilotName = tbPilotName.Text;
		}
		private void tbGroupName_TextChanged(object sender, EventArgs e)
		{
			PropGroupName = tbGroupName.Text.Trim();
		}
		private void cbMSFSServer_SelectedIndexChanged(object sender, EventArgs e)
		{
			PropMSFSServer = cbMSFSServer.Text;
		}
		private void tbNotes_TextChanged(object sender, EventArgs e)
		{
			PropNotes = tbNotes.Text.Trim();
		}

		private void tbRefresh_Leave(object sender, EventArgs e)
		{
			int value = REFRESH_MILLIS_MIN;
			try
			{
				value = int.Parse(tbRefresh.Text);
			}
			catch (FormatException formatException)
			{
				SetMessage(formatException.Message);
			}
			PropRefreshMillis = Math.Max(REFRESH_MILLIS_MIN, value);
			tbRefresh.Text = PropRefreshMillis.ToString();
		}

		/// <summary>Handle users clicking on the About menu option (show a message)</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string version = System.Windows.Forms.Application.ProductVersion;
			MessageBox.Show("Based on Transmitter by Jonathan Beckett\n" +
											"Virtual Flight Online https://virtualflight.online\n\n" +
											"Version " + version + " see https://github.com/Soulsurfer4/\n\n"+
											"Forumlink  https://www.flusi.info/forum/",
											"About Virtual Flight Transmitter Online", 
											MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		/// <summary>Exits the application (shuts things down first)</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			LogInfo("Menu > exit");
			Disconnect();
			if (fsConnector != null)
			{
				fsConnector.Dispose();
				fsConnector = null;
			}
		
			// close the application
			this.Close();
		}


		/// <summary>Resets the textboxes</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void resetSettingsToDefaultsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ResetProps();
			setPropsToDisplay();
			// save the settings
			PropSave();
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

		private void ResetProps()
		{
			PropServerURL = "https://yourserver/send.php";
			PropPin = "1234";
			PropCallsign = "Your Callsign";
			PropPilotName = "Your Name";
			PropGroupName = "Your Group";
			PropMSFSServer = "SOUTH EAST ASIA";
			PropRefreshMillis = 3000;
			PropMagicArgs = "";
		}
		private void PropLoad()
		{
			string[] lines;
			try
			{
				lines = System.IO.File.ReadAllLines("transmitter.ini");
				foreach (string line in lines)
				{
					string[] keyval = line.Split('=');
					if (keyval.Length == 2)
					{
						switch (keyval[0])
						{
							case "ServerURL": PropServerURL = keyval[1]; break;
							case "Pin": PropPin = keyval[1]; break;
							case "RefreshMillis": PropRefreshMillis = Int32.Parse(keyval[1]); break;
							case "Callsign": PropCallsign = keyval[1]; break;
							case "PilotName": PropPilotName = keyval[1]; break;
							case "GroupName": PropGroupName = keyval[1]; break;
							case "MSFSServer": PropMSFSServer = keyval[1]; break;
							case "MagicArgs": PropMagicArgs = keyval[1]; break;
							default:
								break;
						}
					}
				}
			}
			catch (Exception e)
			{
				LogInfo("Loading transmitter.ini failed: " + e.Message);
				return;
			}
			PropRefreshMillis = Math.Max(REFRESH_MILLIS_MIN, PropRefreshMillis);
		}

		private void PropSave()
		{
			try
			{
				System.IO.File.WriteAllText("transmitter.ini",
					"ServerURL=" + PropServerURL + "\r\n" +
					"Pin=" + PropPin + "\r\n" +
					"RefreshMillis=" + PropRefreshMillis + "\r\n" +
					"MSFSServer=" + PropMSFSServer + "\r\n" +
					"Callsign=" + PropCallsign + "\r\n" +
					"PilotName=" + PropPilotName + "\r\n" +
					"GroupName=" + PropGroupName + "\r\n" +
					"MagicArgs=" + PropMagicArgs + "\r\n");
			}
			catch (Exception e)
			{
				LogInfo("Saving transmitter.ini failed: " + e.Message);
				throw;
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
