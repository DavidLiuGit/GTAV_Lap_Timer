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
	public class Main : Script
	{
		#region metadata
		bool firstTime = true;
		string ModName = "Lap Timer";
		string Developer = "iLike2Teabag";
		string Version = "1.0";
		#endregion


		#region main
		public Main()
		{
			Tick += onTick;
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

				readSettings();
			}


			// race mode checkpoint detection
			if (raceMode)
			{
				activeCheckpointDetection();
			}

		}
		#endregion


		// ------------- PROPERTIES/VARIABLES -----------------
		#region properties
		// flags
		bool placementMode = false;
		bool raceMode = false;
		bool lapRace = false;                   // if true, the 1st SectorCheckpoint will be used as the end of a lap

		// hotkeys
		Keys placementActivateKey, addCheckpointKey, undoCheckpointKey, clearCheckpointsKey, exportRaceKey;
		Keys raceActivateKey, restartRaceKey;

		// constants
		IniFile settings = new IniFile("./scripts/LapTimer.ini");
		const float checkpointRadius = 8.0f;    // radius in meters of a checkpoint
		const float checkpointMargin = 1.0f;	// checkpoint's margin multiplier; a checkpoint should be considered reached if player position is within radius * margin from the center of the checkpoint
		Vector3 checkpointOffset = new Vector3(0.0f, 0.0f, -1.0f);	// modify the standard checkpoint's position by this offset when drawing; cosmetic only!

		// placement mode variables
		List<SectorCheckpoint> markedSectorCheckpoints = new List<SectorCheckpoint>();     // add to this list when player marks a position; to be used like a stack (i.e. can only delete/pop latest element!)

		// race mode variables
		SectorCheckpoint activeCheckpoint;		// track the active sector checkpoint
		int freezeTime = 500;
		int activeSector;						// track the active sector number
		int raceStartTime;
		int sectorStartTime;
		Weather weather;

		#endregion


		// ------------- EVENT LISTENERS/HANDLERS -----------------
		#region eventHandlers
		private void onKeyDown(object sender, KeyEventArgs e)
		{
			// enter/exit placement mode with F5
			if (e.KeyCode == placementActivateKey)
				togglePlacementMode();

			// if placement mode is enabled, and the control key was used:
			else if (placementMode && e.Modifiers == Keys.Control)
			{
				// Ctrl+X: add a checkpoint
				if (e.KeyCode == addCheckpointKey)
					markedSectorCheckpoints.Add(createSectorCheckpoint());

				// Ctrl+Z: delete (undo) last SectorCheckpoint
				else if (e.KeyCode == undoCheckpointKey)
					deleteLastSectorCheckpoint();

				// Ctrl+D: clear all SectorCheckpoints, and delete any blips & checkpoints from World
				else if (e.KeyCode == clearCheckpointsKey)
					clearAllSectorCheckpoints();

				else if (e.KeyCode == exportRaceKey)
					exportRace();
			}

			// enter/exit race mode with F6
			else if (e.KeyCode == raceActivateKey)
				toggleRaceMode();

			// if race mode is enabled, and the control key was used:
			else if (raceMode && e.Modifiers == Keys.Control)
			{
				// Ctrl+R: restart race
				if (e.KeyCode == restartRaceKey)
					enterRaceMode();
			}
		}



		/// <summary>
		/// Script destructor. Clean up any objects created to prevent memory leaks.
		/// </summary>
		private void OnShutdown(object sender, EventArgs e)
		{
			clearAllSectorCheckpoints();
			exitRaceMode();
		}
		
		#endregion



		// ------------- TOP LEVEL METHODS -----------------
		#region topLevel

		/// <summary>
		/// Enter/exit "placement mode", which allows user to mark positions as checkpoints in a lap. Can only be entered if raceMode=false
		/// </summary>
		private void togglePlacementMode()
		{
			if (raceMode)
			{
				GTA.UI.Screen.ShowSubtitle("~r~Lap Timer: Cannot enter placement mode while race mode is active.");
				return;
			}

			// if entering placement mode
			if (!placementMode)
			{
				placementMode = true;
				redrawAllSectorCheckpoints();           // if any markedSectorPoints already exist, redraw their blips & checkpoints
				GTA.UI.Screen.ShowSubtitle("Lap Timer: Entering placement mode. Mark sector checkpoints using Ctrl+X.");
			}

			// if exiting placement mode
			else
			{
				hideAllSectorCheckpoints();             // hide blips and checkpoints, but keep the metadata of SectorCheckpoints
				placementMode = false;
				GTA.UI.Screen.ShowSubtitle("Lap Timer: Exiting placement mode.");
			}
		}



		/// <summary>
		/// Enter/exit "race mode", which puts the player into the race they created in placement mode, if saved SectorCheckpoints valid.
		/// </summary>
		/// <returns><c>true</c> if race mode activated successfully; <c>false</c> if deactivated.</returns>
		private bool toggleRaceMode()
		{
			// if currently in race mode, try to leave race mode
			if (raceMode)
			{
				exitRaceMode();
				raceMode = false;
				return false;
			}

			// try to enter race mode
			else
			{
				// if currently in placement mode, attempt to exit it first.
				if (placementMode)
				{
					togglePlacementMode();
					// TODO: check that placement mode was exited successfully
				}

				// check that the player can enter race mode
				if (canEnterRaceMode())
				{
					enterRaceMode();
					raceMode = true;
				}
				else
					GTA.UI.Screen.ShowSubtitle("~r~Lap Timer: cannot enter Race Mode.");
			}
			
			return raceMode;			// return the updated status of the race mode flag
		}



		/// <summary>
		/// Mark player's current position, and create a blip & checkpoint.
		/// </summary>
		/// <returns>Instance of </returns>
		private SectorCheckpoint createSectorCheckpoint(bool verbose = true)
		{
			// instantiate empty SectorCheckpoint
			SectorCheckpoint newCheckpoint = new SectorCheckpoint();

			// determine current player position; decrease pos.Z by 1 meter such that the checkpoint is on the ground
			newCheckpoint.position = Game.Player.Character.Position;
			newCheckpoint.quarternion = Game.Player.Character.Quaternion;

			// assign SectorCheckpoint number, based on length of markedSectorCheckpoints list.
			int checkpointNum = markedSectorCheckpoints.Count;
			newCheckpoint.number = checkpointNum;

			// place marker (blip + checkpoint)
			newCheckpoint.marker = placeMarker(newCheckpoint.position, MarkerType.placement, checkpointNum);

			// print output if verbose
			if (verbose)
				GTA.UI.Screen.ShowSubtitle("Lap Timer: placed checkpoint #" + checkpointNum);

			return newCheckpoint;
		}



		/// <summary>
		/// Race mode only: detect whether the player is within <c>maxDistance</c> of the active checkpoint. Activate next checkpoint and return sector time, if within range.
		/// </summary>
		/// <param name="maxDistance">Maximum distance in meters to trigger checkpoint.</param>
		/// <param name="force3D">Distance computation defaults to 2D (x-y plane only) unless this flag is true</param>
		/// <returns></returns>
		private int activeCheckpointDetection(float maxDistance = checkpointRadius*checkpointMargin, bool force3D = false)
		{
			// get player's position and compute distance to the position of the active checkpoint
			float dist = Game.Player.Character.Position.DistanceTo2D(activeCheckpoint.position);

			try
			{
				// get data on the player's current vehicle
				string vehName = Game.Player.Character.CurrentVehicle.DisplayName;

				// check if it is within the specified maximum
				if (dist < maxDistance)
				{
					// compute time elapsed since race start
					int elapsedTime = Game.GameTime - raceStartTime;

					// save and display elapsed
					TimeType tType = activeCheckpoint.timing.updateTiming(elapsedTime, vehName);
					string notifString = activeCheckpoint.timing.getLatestTimingSummaryString();
					GTA.UI.Notification.Show(string.Format("Checkpoint {0}: ~n~{1}", activeSector, notifString));

					// activate next checkpoint
					activateRaceCheckpoint(activeSector + 1);
				}
			}
			catch
			{
				// player is not longer in a vehicle, or some other exception
				GTA.UI.Screen.ShowSubtitle("Lap Timer: exited vehicle; leaving Race Mode.");
				exitRaceMode();
			}

			return 0;
		}

		#endregion


		// ------------- HELPER METHODS -----------------
		#region helpers
		
		/// <summary>
		/// Place a marker (pair of blip and checkpoint) at the specified position.
		/// </summary>
		/// <param name="pos">Position as instance of Vector3</param>
		/// <param name="type">Indicate whether checkpoint is to be used in a race or in placement mode</param>
		/// <param name="number">Placement mode only: the number to display on the checkpoint</param>
		/// <param name="radius">Radius of the checkpoint</param>
		/// <param name="target">Race mode only: position of the next checkpoint, if applicable. Omit or pass in <c>null</c> if not applicable</param>
		private Marker placeMarker(Vector3 pos, MarkerType type, int number = 0, float radius = checkpointRadius, Vector3? target = null)
		{
			// instantiate empty Marker
			Marker marker = new Marker();

			// place a placement mode checkpoint
			if (type == MarkerType.placement)
			{
				marker.checkpoint = GTA.World.CreateCheckpoint (
									new GTA.CheckpointCustomIcon(CheckpointCustomIconStyle.Number, Convert.ToByte(number)),
									pos+checkpointOffset, pos+checkpointOffset, 
									radius, Color.FromArgb(255, 255, 66));
			}

			// place a regular race checkpoint
			else if (type == MarkerType.raceArrow || type == MarkerType.raceFinish)
			{
				if (type == MarkerType.raceArrow)
					marker.checkpoint = GTA.World.CreateCheckpoint(CheckpointIcon.CylinderDoubleArrow,
						pos + checkpointOffset, checkpointOffset + target ?? new Vector3(0, 0, 0), radius, Color.FromArgb(255, 255, 66));
				else if (type == MarkerType.raceFinish)
					marker.checkpoint = GTA.World.CreateCheckpoint(CheckpointIcon.CylinderCheckerboard,
						pos + checkpointOffset, pos + checkpointOffset, radius, Color.FromArgb(255, 255, 66));
			}

			// create blip
			marker.blip = GTA.World.CreateBlip(pos);
			marker.blip.NumberLabel = number;

			marker.active = true;
			return marker;
		}



		/// <summary>
		/// Clear active markers of a given SectorCheckpoint instance.
		/// </summary>
		/// <param name="chkpt">Instance of <c>SectorCheckpoint</c></param>
		private void hideMarker(SectorCheckpoint chkpt)
		{
			if (chkpt.marker.active)
			{
				chkpt.marker.checkpoint.Delete();    // delete the checkpoint
				chkpt.marker.blip.Delete();                // delete the blip
			}
			chkpt.marker.active = false;
		}


		/// <summary>
		/// Delete the last <c>SectorCheckpoint</c>. First delete its <c>Marker</c>, then remove the checkpoint from <c>markedSectorCheckpoints</c>.
		/// </summary>
		private void deleteLastSectorCheckpoint(bool verbose = true)
		{
			// if markedSectorCheckpoints is empty, do nothing
			if (markedSectorCheckpoints.Count <= 0)
				return;

			// get the last checkpoint in markedSectorCheckpoints
			SectorCheckpoint chkpt = markedSectorCheckpoints.Last();
			int checkpointNum = chkpt.number;

			// delete its Marker (Blip + Checkpoint) from the World, if they are defined
			hideMarker(chkpt);

			// remove the checkpoint from the list
			markedSectorCheckpoints.RemoveAt(markedSectorCheckpoints.Count - 1);

			// print output if verbose
			if (verbose)
				GTA.UI.Screen.ShowSubtitle("Lap Timer: deleted checkpoint #" + checkpointNum);
		}



		/// <summary>
		/// Clear all SectorCheckpoints, and delete all blips & checkpoints from World
		/// </summary>
		private void clearAllSectorCheckpoints(bool verbose = true)
		{
			// iteratively pop saved SectorCheckpoints until the List is empty
			while (markedSectorCheckpoints.Count > 0)
				deleteLastSectorCheckpoint();

			if (verbose)
				GTA.UI.Screen.ShowSubtitle("Lap Timer: All saved SectorCheckpoints cleared. All blips & checkpoints deleted.");
		}



		/// <summary>
		/// Hide the blips and checkpoints of all saved SectorCheckpoints.
		/// </summary>
		private void hideAllSectorCheckpoints()
		{
			for (int i = 0; i < markedSectorCheckpoints.Count; i++)
				hideMarker(markedSectorCheckpoints[i]);
		}

		/// <summary>
		/// Redraw the blips and checkpoints of all saved SectorCheckpoints. Should only be used in placement mode.
		/// </summary>
		private void redrawAllSectorCheckpoints()
		{
			for (int i = 0; i < markedSectorCheckpoints.Count; i++)
			{
				// copy the instance of SectorCheckpoint and replace marker with a new instance returned by placeMarker
				SectorCheckpoint newCheckpoint = markedSectorCheckpoints[i];
				newCheckpoint.marker = placeMarker(markedSectorCheckpoints[i].position, MarkerType.placement, markedSectorCheckpoints[i].number);
				markedSectorCheckpoints[i] = newCheckpoint;                         // assign new instance of SectorCheckpoint to the original index in the List
			}
		}



		/// <summary>
		/// Determine whether the player can enter race mode right now. List all reasons why player cannot enter race mode if not.
		/// </summary>
		/// <returns><c>true</c> if possible to enter race mode</returns>
		private bool canEnterRaceMode()
		{
			bool ret = true;

			// markedSectorCheckpoints must be valid
			if (!validateCheckpoints(markedSectorCheckpoints))
				ret = false;

			// must not be actively on a mission and be able to accept missions
			if (!Game.Player.CanStartMission)
			{
				ret = false;
				GTA.UI.Notification.Show("~r~Lap Timer: Player cannot start mission. Cannot enter Race Mode.");
			}

			// must be in control of character
			if (!Game.Player.CanControlCharacter)
			{
				ret = false;
				GTA.UI.Notification.Show("~r~Lap Timer: Player cannot control character. Cannot enter Race Mode");
			}

			// must be in a vehicle
			if (!Game.Player.Character.IsInVehicle())
			{
				ret = false;
				GTA.UI.Notification.Show("~r~Lap Timer: Player must be in a vehicle to enter Race Mode");				
			}

			return ret;
		}



		/// <summary>
		/// Setup race mode by disabling traffic, clearing weather, and teleporting player to the 1st SectorCheckpoint.
		/// </summary>
		private void enterRaceMode()
		{
			// set the 2nd SectorCheckpoint as active (there must be at least 2 SectorCheckpoints to start race mode); draw the checkpoint
			activateRaceCheckpoint(1);

			// set weather to extra sunny; save current weather so it can be restored after exiting race mode
			weather = World.Weather;
			World.Weather = Weather.ExtraSunny;

			// teleport player to the starting checkpoint; set player orientation
			SectorCheckpoint start = markedSectorCheckpoints[0];
			Game.Player.Character.CurrentVehicle.Position = start.position;
			Game.Player.Character.CurrentVehicle.Quaternion = start.quarternion;

			// freeze time
			Game.Player.CanControlCharacter = false;
			GTA.UI.Screen.ShowSubtitle("~y~Lap Timer: Ready...");
			Script.Wait(freezeTime);
			Game.Player.CanControlCharacter = true;
			GTA.UI.Screen.ShowSubtitle("~g~Lap Timer: Go!");

			// start the clock by getting the current GameTime
			raceStartTime = Game.GameTime;
			sectorStartTime = raceStartTime;
		}

		/// <summary>
		/// Clean up any objects created while in race mode
		/// </summary>
		private void exitRaceMode(bool verbose = true)
		{
			hideMarker(markedSectorCheckpoints[activeSector]);
			// GTA.UI.Screen.ShowSubtitle("Lap Timer: Exiting Race Mode.");

			// try to restore Weather, if possible
			if (weather != null)
				World.Weather = weather;

			raceMode = false;
		}



		/// <summary>
		/// Activate the provided SectorCheckpoint after deactivating the current active checkpoint. 
		/// By activating, a marker will be placed at the checkpoint, and timer will run until player is in range of the checkpoint.
		/// If the index is out of bounds (>= no. of checkpoints), either end the race or reset the lap.
		/// </summary>
		/// <param name="idx">List index of SectorCheckpoint to activate in <c>markedSectorCheckpoints</c></param>
		/// <returns>The now-active SectorCheckpoint</returns>
		private SectorCheckpoint activateRaceCheckpoint(int idx)
		{
			// deactivate current active checkpoint's marker
			try { hideMarker(markedSectorCheckpoints[activeSector]); }
			catch { }
			
			// detect if index is out of expected range
			if (idx >= markedSectorCheckpoints.Count)
			{
				// if point-to-point race, then race is completed. Print time and exit race mode.
				if (!lapRace)
				{
					GTA.UI.Screen.ShowSubtitle("Race completed. ~n~" + activeCheckpoint.timing.getLatestTimingSummaryString(), 10000);
					exitRaceMode();
					return activeCheckpoint;
				}
			}

			// set the new SectorCheckpoint as active (by index)
			activeSector = idx;
			activeCheckpoint = markedSectorCheckpoints[idx];
			bool isFinal = idx == markedSectorCheckpoints.Count - 1;			// determine if this is the final checkpoint based on the index

			// the marker placed should be different, depending on whether this checkpoint is final
			if (isFinal)
				activeCheckpoint.marker = placeMarker(activeCheckpoint.position, MarkerType.raceFinish, idx);

			// if not final checkpoint, place a checkpoint w/ an arrow pointing to the next checkpoint
			else
			{
				Vector3 nextChkptPosition = markedSectorCheckpoints[idx + 1].position;
				activeCheckpoint.marker = placeMarker(activeCheckpoint.position, MarkerType.raceArrow, idx, checkpointRadius, nextChkptPosition);
			}

			return markedSectorCheckpoints[idx];
		}


		/// <summary>
		/// Determine whether the list of saved SectorCheckpooints are valid. Display the failure reason if invalid.
		/// </summary>
		/// <param name="chkpts">List of <c>SectorCheckpoint</c> to validate</param>
		/// <returns><c>true</c> if checkpoints are valid</returns>
		private bool validateCheckpoints(List<SectorCheckpoint> chkpts)
		{
			// there must be 2 or more checkpoints in the list
			if (chkpts.Count < 2)
			{
				GTA.UI.Notification.Show("~r~Lap Timer: Invalid route. You must place at 2 checkpoints in Placement Mode.");
				return false;
			}

			// if all criteria passed, checkpoints are valid
			return true;
		}



		/// <summary>
		/// Convert a time in milliseconds to a readable format. Minutes will be omitted unless forced or >= 60000 ms.
		/// </summary>
		/// <param name="time">Time in milliseconds</param>
		/// <param name="forceMinute">Force inclusion of minutes</param>
		/// <returns></returns>
		public static string msToReadable (int time, bool forceSign = false, bool forceMinute = false) 
		{
			// format milliseconds to seconds (and minutes, if necessary)
			string ret;
			if (forceMinute || time >= 60000)
				ret = TimeSpan.FromMilliseconds(time).ToString(@"m\:s\.fff");
			else ret = TimeSpan.FromMilliseconds(time).ToString(@"s\.fff");

			// prepend sign +/- if necessary, depending on forceSign and time value
			if (forceSign)
				return time >= 0 ? '+' + ret : '-' + ret;
			else
				return time >= 0 ? ret : '-' + ret;
		}



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
			exportRaceKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("exportRace", "Placement") ?? "L");

			// read race mode hotkeys
			raceActivateKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("activate", "Race") ?? "F6");
			restartRaceKey = (Keys)Enum.Parse(typeof(Keys), settings.Read("restartRace", "Race") ?? "R");

			// read race mode settings
			try
			{
				freezeTime = Int32.Parse(settings.Read("freezeTime", "Race"));
			}
			catch (FormatException)
			{
				GTA.UI.Notification.Show("~r~Lap Timer: invalid freeze time setting. Using default setting.");
			}
		}



		/// <summary>
		/// Export the current race to JSON.
		/// </summary>
		private void exportRace()
		{
			// validate checkpoints to make sure the race is valid
			if (!validateCheckpoints(markedSectorCheckpoints))
			{
				GTA.UI.Notification.Show("~r~Lap Timer: cannot export race because validation failed.");
				return;
			}

			// prompt user to enter a name for the race
			string name = GTA.Game.GetUserInput("custom_race");
	
			// export the race using RaceExporter
			string fileName = RaceExporter.writeToJson(RaceExporter.createExportableRace(name, markedSectorCheckpoints, lapRace), name);

			// inform user of the exported file
			GTA.UI.Notification.Show("Lap Timer: exported race as " + fileName);
		}
		#endregion
	}

	


	#region structs
	public class SectorCheckpoint
	{
		// placement data
		public Vector3 position;			// Entity.Position
		public Quaternion quarternion;		// Entity.Quarternion
		public Marker marker;
		public int number;

		// race data - all times are tracked as milliseconds
		public TimingData timing = new TimingData();
	}

	public class Marker
	{
		public Blip blip;
		public Checkpoint checkpoint;
		public bool active = false;
	}

	public enum MarkerType {
		placement,
		raceArrow,
		raceFinish,
		raceAirArrow,
		raceAirFinish,
	}

	public enum TimeType {
		Regular		= 'w',
		VehicleBest	= 'g',		// best time achieved for the current vehicle
		Record		= 'p',		// best time achieved
	}

	public class TimingData
	{
		// records
		public int fastestTime = -1;														// fastest time achieved in any vehicle
		public Dictionary<string, int> vehicleFastestTime = new Dictionary<string, int>();	// track fastest time achieved in each vehicle model
		public int timesCompleted = 0;

		// latest
		public int latestTime;
		public string latestVehicle;
		public int latestRecordSplitTime;
		public int latestVehicleSplitTime;
		public TimeType latestTimeType;


		/// <summary>
		/// Update timing data. Split times will be computed based on the data.
		/// </summary>
		/// <param name="time">Time in milliseconds</param>
		/// <param name="vehicleName">display name of vehicle</param>
		/// <returns></returns>
		public TimeType updateTiming(int time, string vehicleName)
		{
			// set latest time & vehicle
			latestTime = time;
			latestVehicle = vehicleName;
			timesCompleted++;

			// determine the time type & return
			TimeType tType = getLatestTimeType();
			latestTimeType = tType;
			return tType;
		}



		/// <summary>
		/// Get summary of timing, including elapsed time and split times.
		/// </summary>
		/// <returns></returns>
		public string getLatestTimingSummaryString()
		{
			return string.Format("Elapsed: ~{0}~{1} ~n~~s~Fastest split: ~{2}~{3} ~n~~s~Vehicle split: ~{4}~{5}",
				(char)latestTimeType, Main.msToReadable(latestTime),
				latestRecordSplitTime <= 0 ? 'g' : 'r', Main.msToReadable(latestRecordSplitTime, true),
				latestVehicleSplitTime <= 0 ? 'g' : 'r', Main.msToReadable(latestVehicleSplitTime, true));
		}



		/// <summary>
		/// Compute <c>TimeType</c> based on latest timing data, and update records as needed
		/// </summary>
		/// <returns><c>TimeType</c></returns>
		private TimeType getLatestTimeType()
		{
			// check if fastest time; if so, update both fastestTime and vehicleFastestTime
			if (latestTime < fastestTime)
			{
				// compute split times, set new fastest times, and return
				setLatestSplitTimes();
				fastestTime = latestTime;
				vehicleFastestTime[latestVehicle] = latestTime;
				return TimeType.Record;
			}
			else if (fastestTime == -1) fastestTime = latestTime;

			// check if fastest vehicle time
			if (vehicleFastestTime.ContainsKey(latestVehicle))
			{
				if (latestTime < vehicleFastestTime[latestVehicle])
				{
					// compute split times, set new fastest vehicle time, and return
					setLatestSplitTimes();
					vehicleFastestTime[latestVehicle] = latestTime;
					return TimeType.VehicleBest;
				}
			}
			else vehicleFastestTime[latestVehicle] = latestTime;

			setLatestSplitTimes();
			return TimeType.Regular;
		}



		/// <summary>
		/// Compute and set split times, based on latest timing data.
		/// </summary>
		private void setLatestSplitTimes() {
			// compute & set record split time
			latestRecordSplitTime = latestTime - fastestTime;

			// compute & set vehicle split time, if possible
			if (vehicleFastestTime.ContainsKey(latestVehicle))
				latestVehicleSplitTime = latestTime - vehicleFastestTime[latestVehicle];
			else latestVehicleSplitTime = 0;
		}
	}

	#endregion
}

namespace LapTimer
{

}
// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net