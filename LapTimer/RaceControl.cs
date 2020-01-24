using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using GTA;
using GTA.Native;
using GTA.Math;


namespace LapTimer
{
	class RaceControl
	{
		#region properties

		// flags
		public bool placementMode = false;
		public bool raceMode = false;
		public bool lapRace = false;                   // if true, the 1st SectorCheckpoint will be used as the end of a lap

		// constants
		const float checkpointMargin = 1.0f;	// checkpoint's margin multiplier; a checkpoint should be considered reached if player position is within radius * margin from the center of the checkpoint
		readonly Vector3 checkpointOffset = new Vector3(0.0f, 0.0f, -1.0f);	// modify the standard checkpoint's position by this offset when drawing; cosmetic only!

		// placement mode variables
		public List<SectorCheckpoint> markedSectorCheckpoints = new List<SectorCheckpoint>();     // add to this list when player marks a position; to be used like a stack (i.e. can only delete/pop latest element!)

		// race mode variables
		public SectorCheckpoint activeCheckpoint;		// track the active sector checkpoint
		public int freezeTime = 500;
		public int activeSector;						// track the active sector number
		public int raceStartTime;
		public Weather weather;

		#endregion


		#region topLevel

		/// <summary>
		/// Enter/exit "placement mode", which allows user to mark positions as checkpoints in a lap. Can only be entered if raceMode=false
		/// </summary>
		public void togglePlacementMode()
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
		public bool toggleRaceMode()
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
		public SectorCheckpoint createSectorCheckpoint(bool verbose = true)
		{
			// instantiate empty SectorCheckpoint
			int checkpointNum = markedSectorCheckpoints.Count;
			SectorCheckpoint newCheckpoint = new SectorCheckpoint(checkpointNum);
			markedSectorCheckpoints.Add(newCheckpoint);

			return newCheckpoint;
		}



		/// <summary>
		/// Race mode only: detect whether the player is within <c>maxDistance</c> of the active checkpoint. Activate next checkpoint and return sector time, if within range.
		/// </summary>
		/// <param name="maxDistance">Maximum distance in meters to trigger checkpoint.</param>
		/// <param name="force3D">Distance computation defaults to 2D (x-y plane only) unless this flag is true</param>
		/// <returns></returns>
		public int activeCheckpointDetection(float margin = checkpointMargin, bool force3D = false)
		{
			// get player's position and compute distance to the position of the active checkpoint
			float dist = Game.Player.Character.Position.DistanceTo2D(activeCheckpoint.position);

			try
			{
				// get data on the player's current vehicle
				string vehName = Game.Player.Character.CurrentVehicle.DisplayName;

				// check if it is within the specified maximum (margin * checkpointRadius)
				if (dist < margin * SectorCheckpoint.checkpointRadius)
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



		/// <summary>
		/// Delete the last <c>SectorCheckpoint</c>. First delete its <c>Marker</c>, then remove the checkpoint from <c>markedSectorCheckpoints</c>.
		/// </summary>
		public void deleteLastSectorCheckpoint(bool verbose = true)
		{
			// if markedSectorCheckpoints is empty, do nothing
			if (markedSectorCheckpoints.Count <= 0)
				return;

			// get the last checkpoint in markedSectorCheckpoints
			SectorCheckpoint chkpt = markedSectorCheckpoints.Last();
			int checkpointNum = chkpt.number;

			// delete its Marker (Blip + Checkpoint) from the World, if they are defined
			chkpt.hideMarker();

			// remove the checkpoint from the list
			markedSectorCheckpoints.RemoveAt(markedSectorCheckpoints.Count - 1);

			// print output if verbose
			if (verbose)
				GTA.UI.Screen.ShowSubtitle("Lap Timer: deleted checkpoint #" + checkpointNum);
		}



		/// <summary>
		/// Clear all SectorCheckpoints, and delete all blips & checkpoints from World
		/// </summary>
		public void clearAllSectorCheckpoints(bool verbose = true)
		{
			// iteratively pop saved SectorCheckpoints until the List is empty
			while (markedSectorCheckpoints.Count > 0)
				deleteLastSectorCheckpoint();

			if (verbose)
				GTA.UI.Screen.ShowSubtitle("Lap Timer: All saved SectorCheckpoints cleared. All blips & checkpoints deleted.");
		}



		/// <summary>
		/// Setup race mode by disabling traffic, clearing weather, and teleporting player to the 1st SectorCheckpoint.
		/// </summary>
		public void enterRaceMode()
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
		}

		/// <summary>
		/// Clean up any objects created while in race mode
		/// </summary>
		public void exitRaceMode(bool verbose = true)
		{
			markedSectorCheckpoints[activeSector].hideMarker();
			// GTA.UI.Screen.ShowSubtitle("Lap Timer: Exiting Race Mode.");

			// try to restore Weather, if possible
			if (weather != null)
				World.Weather = weather;

			raceMode = false;
		}

		#endregion



		#region helpers

		/// <summary>
		/// Hide the blips and checkpoints of all saved SectorCheckpoints.
		/// </summary>
		private void hideAllSectorCheckpoints()
		{
			for (int i = 0; i < markedSectorCheckpoints.Count; i++)
				markedSectorCheckpoints[i].hideMarker();
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
				newCheckpoint.marker = newCheckpoint.placeMarker(MarkerType.placement, markedSectorCheckpoints[i].number);
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
		/// Activate the provided SectorCheckpoint after deactivating the current active checkpoint. 
		/// By activating, a marker will be placed at the checkpoint, and timer will run until player is in range of the checkpoint.
		/// If the index is out of bounds (>= no. of checkpoints), either end the race or reset the lap.
		/// </summary>
		/// <param name="idx">List index of SectorCheckpoint to activate in <c>markedSectorCheckpoints</c></param>
		/// <returns>The now-active SectorCheckpoint</returns>
		private SectorCheckpoint activateRaceCheckpoint(int idx)
		{
			// deactivate current active checkpoint's marker
			try { markedSectorCheckpoints[activeSector].hideMarker(); }
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
				activeCheckpoint.marker = activeCheckpoint.placeMarker(MarkerType.raceFinish, idx);

			// if not final checkpoint, place a checkpoint w/ an arrow pointing to the next checkpoint
			else
			{
				Vector3 nextChkptPosition = markedSectorCheckpoints[idx + 1].position;
				activeCheckpoint.marker = activeCheckpoint.placeMarker(MarkerType.raceArrow, idx, nextChkptPosition);
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

		#endregion



		#region I/O

		/// <summary>
		/// Export the current race to JSON. User will be prompted to enter a name.
		/// </summary>
		public void exportRace()
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
			string fileName = RaceExporter.serializeToJson(RaceExporter.createExportableRace(name, markedSectorCheckpoints, lapRace), name);

			// inform user of the exported file
			GTA.UI.Notification.Show("Lap Timer: exported race as " + fileName);
		}



		/// <summary>
		/// Import a race from a file on disk. The currently placed checkpoints will be overwritten.
		/// </summary>
		public void importRace()
		{
			// clean up any existing race/checkpoints
			clearAllSectorCheckpoints();

			// prompt user to enter the name of the file (with or without the file extension) to import from
			string name = GTA.Game.GetUserInput("custom_race");

			try
			{
				// attempt to import from file
				ExportableRace race = RaceExporter.deserializeFromJson(name);

				// repopulate List<SectorCheckpoint> using the imported race data
				lapRace = race.lapMode;
				for (int i = 0; i < race.checkpoints.Length; i++)
				{
					SimplifiedCheckpoint sc = race.checkpoints[i];
					SectorCheckpoint chkpt = new SectorCheckpoint(sc.number, sc.position, sc.quarternion, false);
					markedSectorCheckpoints.Add(chkpt);
				}

				// inform user of successful load
				GTA.UI.Screen.ShowSubtitle("Lap Timer: successfully imported race!");
			}
			catch { }
		}

		#endregion
	}
}
