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
				readSettings(base.Settings);
				menu = new NativeUIMenu(ref race);
				KeyDown += onKeyDown;
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
		Keys placementActivateKey, addCheckpointKey, undoCheckpointKey, clearCheckpointsKey;
		Keys raceActivateKey, restartRaceKey;
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
		private void readSettings(ScriptSettings ss)
		{
			// read & parse placement mode hotkeys
			string section = "Placement";
			placementActivateKey = ss.GetValue<Keys>(section, "activate", Keys.F5);
			addCheckpointKey = ss.GetValue<Keys>(section, "addCheckpoint", Keys.X);
			undoCheckpointKey = ss.GetValue<Keys>(section, "undoCheckpoint", Keys.Z);
			clearCheckpointsKey = ss.GetValue<Keys>(section, "clearCheckpoints", Keys.D);

			// read race mode hotkeys
			section = "Race";
			raceActivateKey = ss.GetValue<Keys>(section, "activate", Keys.F6);
			restartRaceKey = ss.GetValue<Keys>(section, "restartRace", Keys.R);
			race.freezeTime = ss.GetValue<int>(section, "freezeTime", 750);

			// read Script hotkeys
			section = "Script";
			menuKey = ss.GetValue<Keys>(section, "menu", Keys.N);
		}

		#endregion
	}

}

// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net