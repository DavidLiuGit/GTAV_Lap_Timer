// LapTimer 1.0 - Abel Software
// You must download and use Scripthook V Dot Net Reference (LINKS AT BOTTOM OF THE TEMPLATE)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Specialized;
using GTA;
using GTA.Native;
using GTA.Math;


namespace LapTimer
{
	public class RaceTimerMain : Script
	{
		#region metadata
		bool firstTime = true;
		string ModName = "Lap Timer";
		string Developer = "iLike2Teabag";
		string Version = "1.0";
		#endregion


		#region main
		public RaceTimerMain()
		{
			Tick += onTick;
			Tick += (o, e) => menu._menuPool.ProcessMenus();
			KeyDown += onKeyDown;
			Interval = 1;
			Aborted += OnShutdown;
		}

		private void onTick(object sender, EventArgs e)
		{
			if (firstTime) // if this is the users first time loading the mod, this information will appear
			{
				GTA.UI.Screen.ShowSubtitle(ModName + " " + Version + " by " + Developer + " Loaded");
				firstTime = false;

				// setup tasks
				race = new RaceControl();
				readSettings();
				menu = new NativeUIMenu(ref race);
			}


			// race mode checkpoint detection
			if (race.raceMode)
			{
				race.activeCheckpointDetection();
			}

		}
		#endregion


		// ------------- PROPERTIES/VARIABLES -----------------
		#region properties

		// references
		NativeUIMenu menu;
		RaceControl race;

		// hotkeys
		Keys menuKey;
		Keys placementActivateKey, addCheckpointKey, undoCheckpointKey, clearCheckpointsKey, exportRaceKey, importRaceKey;
		Keys raceActivateKey, restartRaceKey;

		// constants
		IniFile settings = new IniFile("./scripts/LapTimer.ini");

		#endregion


		// ------------- EVENT LISTENERS/HANDLERS -----------------
		#region eventHandlers
		private void onKeyDown(object sender, KeyEventArgs e)
		{
			// open menu
			if (e.Modifiers == Keys.Control && e.KeyCode == menuKey)
				menu.toggleMenu();

			// enter/exit placement mode with F5
			else if (e.KeyCode == placementActivateKey)
				race.togglePlacementMode();

			// if placement mode is enabled, and the control key was used:
			else if (race.placementMode && e.Modifiers == Keys.Control)
			{
				// Ctrl+X: add a checkpoint
				if (e.KeyCode == addCheckpointKey)
					race.createSectorCheckpoint();

				// Ctrl+Z: delete (undo) last SectorCheckpoint
				else if (e.KeyCode == undoCheckpointKey)
					race.deleteLastSectorCheckpoint();

				// Ctrl+D: clear all SectorCheckpoints, and delete any blips & checkpoints from World
				else if (e.KeyCode == clearCheckpointsKey)
					race.clearAllSectorCheckpoints();

				// export current race to JSON file
				else if (e.KeyCode == exportRaceKey)
					race.exportRace();

				// import a race from a JSON file
				else if (e.KeyCode == importRaceKey)
					race.importRace();
			}

			// enter/exit race mode with F6
			else if (e.KeyCode == raceActivateKey)
				race.toggleRaceMode();

			// if race mode is enabled, and the control key was used:
			else if (race.raceMode && e.Modifiers == Keys.Control)
			{
				// Ctrl+R: restart race
				if (e.KeyCode == restartRaceKey)
					race.enterRaceMode();
			}
		}



		/// <summary>
		/// Script destructor. Clean up any objects created to prevent memory leaks.
		/// </summary>
		private void OnShutdown(object sender, EventArgs e)
		{
			race.clearAllSectorCheckpoints();
			race.exitRaceMode();
		}
		
		#endregion

		

		// ------------- HELPER METHODS -----------------
		#region helpers
		

		/// <summary>
		/// Read in INI key settings. Includes default settings if INI read fails.
		/// </summary>
		private void readSettings()
		{
			// read & parse placement mode hotkeys
			placementActivateKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("activate", "Placement") ?? "F5");
			addCheckpointKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("addCheckpoint", "Placement") ?? "X");
			undoCheckpointKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("undoCheckpoint", "Placement") ?? "Z");
			clearCheckpointsKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("clearCheckpoints", "Placement") ?? "D");
			exportRaceKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("exportRace", "Placement") ?? "O");
			importRaceKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("importRaceKey", "Placement") ?? "I");

			// read race mode hotkeys
			raceActivateKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("activate", "Race") ?? "F6");
			restartRaceKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("restartRace", "Race") ?? "R");

			// read Script hotkeys
			menuKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("menu", "Script") ?? "N");

			// read race mode settings
			try
			{
				race.freezeTime = Int32.Parse(settings.Read("freezeTime", "Race"));
			}
			catch (FormatException)
			{
				GTA.UI.Notification.Show("~r~Lap Timer: invalid freeze time setting. Using default setting.");
			}
		}

		#endregion
	}

}

// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net