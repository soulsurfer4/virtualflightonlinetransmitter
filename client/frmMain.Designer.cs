using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using TextBox = System.Windows.Forms.TextBox;
using ComboBox = System.Windows.Forms.ComboBox;
using Button = System.Windows.Forms.Button;

namespace VirtualFlightOnlineTransmitter
{
	partial class frmMain
	{
		private static AnchorStyles ANCHOR_TLR = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		private static Font FONT = new Font("Courier New", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Disconnect(); // Close all Timer
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// default constants            
			this.components = new System.ComponentModel.Container();

			tmrTransmit = new System.Windows.Forms.Timer(this.components);
			tmrTransmit.Interval = TICK_MILLIS;
			tmrTransmit.Tick += new System.EventHandler(this.OnTick);

			ComponentResourceManager resources = new ComponentResourceManager(typeof(frmMain));
			this.mnuMain = new MenuStrip();
			this.mnuMain.SuspendLayout();
			this.gbUserInfo = new GroupBox();
			this.gbUserInfo.SuspendLayout();
			this.gbAircraftData = new GroupBox();
			this.gbAircraftData.SuspendLayout();

			this.transmitterToolStripMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem4 = new ToolStripSeparator();
			this.resetSettingsToDefaultsToolStripMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem2 = new ToolStripSeparator();
			this.exitToolStripMenuItem = new ToolStripMenuItem();
			this.viewToolStripMenuItem = new ToolStripMenuItem();
			this.aircraftDataToolStripMenuItem = new ToolStripMenuItem();
			this.helpToolStripMenuItem = new ToolStripMenuItem();
			this.aboutToolStripMenuItem = new ToolStripMenuItem();

			this.SuspendLayout();
			// 
			// lbServerURL
			// 
			this.lbServer = NewLabelAt("Server URL");

			this.tbServerURL = NewTextAt(true);
			this.tbServerURL.TabIndex = 0;
			this.tbServerURL.TextChanged += new System.EventHandler(this.tbServerURL_TextChanged);
			this.tbServerURL.Leave += new System.EventHandler(this.tbServerURL_Leave);

			// 
			// tbPin
			// 
			this.lbPin = NewLabelAt("Pin");

			this.tbPin = NewTextAt(true);
			this.tbPin.PasswordChar = '*';
			this.tbPin.TabIndex = 1;
			this.tbPin.TextChanged += new System.EventHandler(this.tbPin_TextChanged);
			this.tbPin.Leave += new System.EventHandler(this.tbPin_Leave);

			// Refresh rate
			this.lbRefresh = NewLabelAt("Send every ms");

			this.tbRefresh = NewTextAt(true);
			this.tbRefresh.TabIndex = 2;
			this.tbRefresh.TextAlign = HorizontalAlignment.Right;
			this.tbRefresh.Leave += new System.EventHandler(tbRefresh_Leave);

			// 
			// lbCallsign
			// 
			this.lbCallsign = NewLabelAt("Callsign");

			this.tbCallsign = NewTextAt(true);
			this.tbCallsign.TabIndex = 3;
			this.tbCallsign.TextChanged += new System.EventHandler(this.tbCallsign_TextChanged);
			this.tbCallsign.Leave += new System.EventHandler(this.tbCallsign_Leave);

			// 
			// lbPilotName
			// 
			this.lbPilotName = NewLabelAt("Pilot Name");
			this.tbPilotName = NewTextAt(true);
			this.tbPilotName.TabIndex = 4;
			this.tbPilotName.TextChanged += new System.EventHandler(this.tbPilotName_TextChanged);
			this.tbPilotName.Leave += new System.EventHandler(this.tbPilotName_Leave);

			// lbGroupName
			this.lbGroupName = NewLabelAt("Group Name");
			this.tbGroupName = NewTextAt(true);
			this.tbGroupName.TabIndex = 5;
			this.tbGroupName.TextChanged += new System.EventHandler(tbGroupName_TextChanged);
			this.tbGroupName.Leave += new System.EventHandler(tbGroupName_Leave);

			// 
			// cbMSFSServer
			// 
			this.lbMSFSServer = NewLabelAt("Server");

			this.cbMSFSServer = new ComboBox();
			this.cbMSFSServer.FlatStyle = FlatStyle.System;
			this.cbMSFSServer.Font = FONT;
			this.cbMSFSServer.FormattingEnabled = true;
			this.cbMSFSServer.Items.AddRange(new object[] { "NORTH EUROPE", "WEST EUROPE", "EAST USA", "WEST USA", "SOUTH EAST ASIA" });
			this.cbMSFSServer.TabIndex = 6;
			this.cbMSFSServer.SelectedIndexChanged += new System.EventHandler(this.cbMSFSServer_SelectedIndexChanged);

			// lbNotes
			this.lbNotes = NewLabelAt("Notes");

			this.tbNotes = NewTextAt(true);
			this.tbNotes.MaxLength = 1024;
			this.tbNotes.Multiline = true;
			this.tbNotes.ScrollBars = ScrollBars.Vertical;
			this.tbNotes.TabIndex = 7;
			this.tbNotes.TextChanged += new System.EventHandler(tbNotes_TextChanged);
			this.tbNotes.Leave += new System.EventHandler(tbNotes_Leave);

			// 
			// btnConnect
			this.btnConnect = new Button();
			this.btnConnect.TabIndex = 8;
			this.btnConnect.Text = "Switch On";
			this.btnConnect.UseVisualStyleBackColor = true;
			this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);

			// btnDisconnect
			this.btnDisconnect = new Button();
			this.btnDisconnect.TabIndex = 9;
			this.btnDisconnect.Text = "Turn Off";
			this.btnDisconnect.UseVisualStyleBackColor = true;
			this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
			this.btnDisconnect.Enabled = false;

			// tsslMain - Information at buttom
			this.tsslMain = new ToolStripStatusLabel();
			this.tsslMain.Font = FONT;
			this.tsslMain.Size = new Size(16, 17);
			this.tsslMain.Text = "";
			// ssMain
			this.ssMain = new StatusStrip();
			this.ssMain.SuspendLayout();
			this.ssMain.ImageScalingSize = new Size(24, 24);
			this.ssMain.Items.AddRange(new ToolStripItem[] { this.tsslMain });
			this.ssMain.TabIndex = 5;
			this.ssMain.Text = "...";

			/////////////////////////////////////////////////////////////
			// lbAircraftType
			this.lbAircraftType = NewLabelAt("Aircraft Type");
			this.tbAircraftType = NewTextAt(false);

			// lbTouchdownVelocity
			this.lbTouchdownVel = NewLabelAt("Landing Rate");
			this.tbTouchdownVel = NewTextAt(false);

			// lbGroundspeed
			this.lbGroundspeed = NewLabelAt("Groundspeed");
			this.tbGroundspeed = NewTextAt(false);

			// lbHeading
			this.lbHeading = NewLabelAt("Heading");
			this.tbHeading = NewTextAt(false);

			// 
			// lbAirspeed
			// 
			this.lbAirspeed = NewLabelAt("Airspeed");
			this.tbAirspeed = NewTextAt(false);

			// tbAltitude
			this.lbAltitude = NewLabelAt("Altitude");
			this.tbAltitude = NewTextAt(false);

			// tbLatitude
			this.lbLatitude = NewLabelAt("Latitude");
			this.tbLatitude = NewTextAt(false);

			// tbLongitude
			this.lbLongitude = NewLabelAt("Longitude");
			this.tbLongitude = NewTextAt(false);

			// Layout labels and boxes
			int xLabel = 7; int hLabel = 13;
			int xBox = 89; int hBox = 22; int wBox = 307;

			int tLine = 23;
			SetLayout(lbServer, xLabel, tLine, 63, hLabel); SetLayout(tbServerURL, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbPin, xLabel, tLine, 22, hLabel); SetLayout(tbPin, xBox, tLine - 4, 51, hBox);
			SetLayout(lbRefresh, xBox + 142, tLine, 80, hLabel); SetLayout(tbRefresh, xBox + 247, tLine - 4, 60, hBox); tLine += 28;

			SetLayout(lbCallsign, xLabel, tLine, 43, hLabel); SetLayout(tbCallsign, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbPilotName, xLabel, tLine, 58, hLabel); SetLayout(tbPilotName, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbGroupName, xLabel, tLine, 67, hLabel); SetLayout(tbGroupName, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbMSFSServer, xLabel, tLine, 38, hLabel); SetLayout(cbMSFSServer, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbNotes, xLabel, tLine, 55, hLabel); SetLayout(tbNotes, xBox, tLine - 4, wBox, 55);
			tLine += 100;
			SetLayout(btnConnect, 12, tLine, 203, 23); SetLayout(btnDisconnect, 217, tLine, 199, 23);

			tLine = 23;
			SetLayout(lbAircraftType, xLabel, tLine, 67, hLabel); SetLayout(tbAircraftType, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbLatitude, xLabel, tLine, 45, hLabel); SetLayout(tbLatitude, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbLongitude, xLabel, tLine, 67, hLabel); SetLayout(tbLongitude, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbAltitude, xLabel, tLine, 67, hLabel); SetLayout(tbAltitude, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbHeading, xLabel, tLine, 67, hLabel); SetLayout(tbHeading, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbAirspeed, xLabel, tLine, 67, hLabel); SetLayout(tbAirspeed, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbGroundspeed, xLabel, tLine, 67, hLabel); SetLayout(tbGroundspeed, xBox, tLine - 4, wBox, hBox); tLine += 28;
			SetLayout(lbTouchdownVel, xLabel, tLine, 71, hLabel); SetLayout(tbTouchdownVel, xBox, tLine - 4, wBox, hBox);

			// 
			// mnuMain
			// 
			this.mnuMain.ImageScalingSize = new Size(24, 24);
			this.mnuMain.Items.AddRange(new ToolStripItem[] {
			this.transmitterToolStripMenuItem,
			this.viewToolStripMenuItem,
			this.helpToolStripMenuItem});
			this.mnuMain.Location = new Point(0, 0);
			this.mnuMain.Padding = new Padding(4, 1, 0, 1);
			this.mnuMain.Size = new Size(424, 24);
			this.mnuMain.TabIndex = 4;
			this.mnuMain.Text = "...";
			// 
			// transmitterToolStripMenuItem
			// 
			this.transmitterToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
						this.toolStripMenuItem4,
						this.resetSettingsToDefaultsToolStripMenuItem,
						this.toolStripMenuItem2,
						this.exitToolStripMenuItem
			});
			this.transmitterToolStripMenuItem.Size = new Size(37, 22);
			this.transmitterToolStripMenuItem.Text = "&File";
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new Size(204, 6);
			// 
			// resetSettingsToDefaultsToolStripMenuItem
			// 
			this.resetSettingsToDefaultsToolStripMenuItem.Size = new Size(207, 22);
			this.resetSettingsToDefaultsToolStripMenuItem.Text = "&Reset Settings to Defaults";
			this.resetSettingsToDefaultsToolStripMenuItem.Click += new System.EventHandler(this.resetSettingsToDefaultsToolStripMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new Size(204, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new Size(207, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem1_Click);
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
			this.aircraftDataToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new Size(44, 22);
			this.viewToolStripMenuItem.Text = "&View";
			// 
			// aircraftDataToolStripMenuItem
			// 
			this.aircraftDataToolStripMenuItem.CheckOnClick = true;
			this.aircraftDataToolStripMenuItem.Name = "aircraftDataToolStripMenuItem";
			this.aircraftDataToolStripMenuItem.Size = new Size(180, 22);
			this.aircraftDataToolStripMenuItem.Text = "&Aircraft Data";
			this.aircraftDataToolStripMenuItem.Click += new System.EventHandler(this.aircraftDataToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { this.aboutToolStripMenuItem });
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new Size(44, 22);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new Size(107, 22);
			this.aboutToolStripMenuItem.Text = "&About";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);

			// 
			// gbUserInfo
			// 
			this.gbUserInfo.Anchor = ANCHOR_TLR;
			this.gbUserInfo.Controls.Add(this.lbServer);
			this.gbUserInfo.Controls.Add(this.tbServerURL);
			this.gbUserInfo.Controls.Add(this.lbPin);
			this.gbUserInfo.Controls.Add(this.tbPin);
			this.gbUserInfo.Controls.Add(this.lbRefresh);
			this.gbUserInfo.Controls.Add(this.tbRefresh);
			this.gbUserInfo.Controls.Add(this.lbMSFSServer);
			this.gbUserInfo.Controls.Add(this.cbMSFSServer);
			this.gbUserInfo.Controls.Add(this.lbGroupName);
			this.gbUserInfo.Controls.Add(this.tbGroupName);
			this.gbUserInfo.Controls.Add(this.lbPilotName);
			this.gbUserInfo.Controls.Add(this.tbPilotName);
			this.gbUserInfo.Controls.Add(this.lbCallsign);
			this.gbUserInfo.Controls.Add(this.tbCallsign);
			this.gbUserInfo.Controls.Add(this.lbNotes);
			this.gbUserInfo.Controls.Add(this.tbNotes);

			SetLayout(gbUserInfo, 12, 35, 403, 249);
			this.gbUserInfo.TabStop = false;
			this.gbUserInfo.Text = "User Information";
			// 
			// gbAircraftData
			// 
			this.gbAircraftData.Anchor = ANCHOR_TLR;
			this.gbAircraftData.Controls.Add(this.lbAircraftType);
			this.gbAircraftData.Controls.Add(this.tbAircraftType);

			this.gbAircraftData.Controls.Add(this.lbTouchdownVel);
			this.gbAircraftData.Controls.Add(this.tbTouchdownVel);

			this.gbAircraftData.Controls.Add(this.lbGroundspeed);
			this.gbAircraftData.Controls.Add(this.tbGroundspeed);

			this.gbAircraftData.Controls.Add(this.lbHeading);
			this.gbAircraftData.Controls.Add(this.tbHeading);

			this.gbAircraftData.Controls.Add(this.lbAirspeed);
			this.gbAircraftData.Controls.Add(this.tbAirspeed);

			this.gbAircraftData.Controls.Add(this.lbAltitude);
			this.gbAircraftData.Controls.Add(this.tbAltitude);
			this.gbAircraftData.Controls.Add(this.tbLatitude);
			this.gbAircraftData.Controls.Add(this.tbLongitude);
			this.gbAircraftData.Controls.Add(this.lbLatitude);
			this.gbAircraftData.Controls.Add(this.lbLongitude);

			SetLayout(this.gbAircraftData, 12, 340, 403, 246);
			this.gbAircraftData.TabIndex = 3;
			this.gbAircraftData.TabStop = false;
			this.gbAircraftData.Text = "Aircraft Data";
			// 
			// frmMain
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(424, 344);
			this.MaximumSize = new Size(440, 658);
			this.MinimumSize = new Size(440, 354);

			this.Controls.Add(this.mnuMain);
			this.Controls.Add(this.gbUserInfo);
			this.Controls.Add(this.btnDisconnect);
			this.Controls.Add(this.btnConnect);
			this.Controls.Add(this.ssMain);
			this.Controls.Add(this.gbAircraftData);

			this.Icon = ((Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.mnuMain;
			this.Name = "frmMain";
			this.Text = frmMain.Application_Title;
			this.FormClosing += new FormClosingEventHandler(this.Main_FormClosing);
			this.Load += new System.EventHandler(this.Main_Load);

			this.gbUserInfo.ResumeLayout(false);
			this.gbUserInfo.PerformLayout();
			this.gbAircraftData.ResumeLayout(false);
			this.gbAircraftData.PerformLayout();
			this.mnuMain.ResumeLayout(false);
			this.mnuMain.PerformLayout();
			this.ssMain.ResumeLayout(false);
			this.ssMain.PerformLayout();
			ssMain.Location = new Point(0, 332);
			ssMain.Size = new Size(424, hBox);
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		private Label NewLabelAt(string title)
		{
			Label label = new Label();
			label.AutoSize = true;
			label.Text = title;
			return label;
		}

		private TextBox NewTextAt(bool enabled)
		{
			TextBox textBox = new TextBox();
			textBox.Enabled = enabled;
			textBox.Font = FONT;
			return textBox;
		}

		private void SetLayout(Control ctrl, int top, int left, int width, int height)
		{
			ctrl.Anchor = ANCHOR_TLR;
			ctrl.Location = new Point(top, left);
			ctrl.Size = new Size(width, height);
		}
		#endregion
		private Timer tmrTransmit;

		private Button btnConnect;
		private GroupBox gbUserInfo;
		private Label lbCallsign;
		private TextBox tbCallsign;
		private Label lbPilotName;
		private TextBox tbPilotName;
		private Label lbNotes;
		private TextBox tbNotes;
		private Label lbTouchdownVel;
		private TextBox tbTouchdownVel;
		private Label lbAircraftType;
		private TextBox tbAircraftType;
		private Label lbMSFSServer;
		private ComboBox cbMSFSServer;
		private Label lbServer;
		private TextBox tbServerURL;
		private Label lbPin;
		private TextBox tbPin;
		// new label and field for custom refresh rates, Soulsurfer, 11.01.2025
		private Label lbRefresh;
		private TextBox tbRefresh;

		private GroupBox gbAircraftData;
		private Label lbAirspeed;
		private TextBox tbAirspeed;
		private Label lbAltitude;
		private TextBox tbAltitude;
		private Label lbLongitude;
		private TextBox tbLongitude;
		private Label lbLatitude;
		private TextBox tbLatitude;
		private Label lbHeading;
		private TextBox tbHeading;
		private Label lbGroupName;
		private TextBox tbGroupName;
		private Label lbGroundspeed;
		private TextBox tbGroundspeed;

		private StatusStrip ssMain;

		private MenuStrip mnuMain;
		private ToolStripMenuItem helpToolStripMenuItem;
		private ToolStripMenuItem aboutToolStripMenuItem;
		private ToolStripMenuItem transmitterToolStripMenuItem;
		private ToolStripMenuItem resetSettingsToDefaultsToolStripMenuItem;
		private ToolStripSeparator toolStripMenuItem2;
		private ToolStripMenuItem exitToolStripMenuItem;

		private ToolStripMenuItem viewToolStripMenuItem;
		private ToolStripMenuItem aircraftDataToolStripMenuItem;
		private ToolStripStatusLabel tsslMain;
		private Button btnDisconnect;
		private ToolStripSeparator toolStripMenuItem4;
	}
}