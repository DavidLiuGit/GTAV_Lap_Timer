// LapTimer 1.0 - Abel Software
// You must download and use Scripthook V Dot Net Reference (LINKS AT BOTTOM OF THE TEMPLATE)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
			}
			// If the user has used the current mod version before, the text (and code) above will not appear

			// ------------- ANY CODE PLACED ABOVE THIS LINE WILL HAPPEN WITH EVERY TICK (1 MS) OF THE SCRIPT -----------------
		}


		// ------------- PROPERTIES/VARIABLES -----------------
		// flags
		bool placementMode = false;
		bool raceMode = false;
		bool lapRace = false;                   // if true, the 1st SectorCheckpoint will be used as the end of a lap

		// constants
		const float checkpointRadius = 8.0f;    // radius in meters of a checkpoint
		Vector3 checkpointOffset = new Vector3(0.0f, 0.0f, -1.0f);	// modify the standard checkpoint's position by this offset when drawing; cosmetic only!

		// placement mode variables
		List<SectorCheckpoint> markedSectorCheckpoints = new List<SectorCheckpoint>();     // add to this list when player marks a position; to be used like a stack (i.e. can only delete/pop latest element!)

		// race mode variables
		SectorCheckpoint activeCheckpoint;
		int activeSector;						// track the active sector number



		// ------------- EVENT LISTENERS/HANDLERS -----------------
		#region eventHandlers
		private void onKeyDown(object sender, KeyEventArgs e)
		{
			// enter/exit placement mode with F5
			if (e.KeyCode == Keys.F5)
			{
				togglePlacementMode();
			}

			// if placement mode is enabled, and the control key was used:
			else if (placementMode && e.Modifiers == Keys.Control)
			{
				switch (e.KeyCode)
				{
					// Ctrl+X: add a checkpoint
					case Keys.X:
						markedSectorCheckpoints.Add(createSectorCheckpoint());
						break;

					// Ctrl+Z: delete (undo) last SectorCheckpoint
					case Keys.Z:
						deleteLastSectorCheckpoint();
						break;

					// Ctrl+D: clear all SectorCheckpoints, and delete any blips & checkpoints from World
					case Keys.D:
						clearAllSectorCheckpoints();
						break;

					// Ctrl+L: toggle Lap Race mode
					case Keys.L:
						lapRace = !lapRace;
						GTA.UI.Screen.ShowSubtitle("Lap Timer: lap race flag: " + lapRace);
						break;
				}
			}

			// enter/exit race mode with F6
			else if (e.KeyCode == Keys.F6)
			{
				toggleRaceMode();
			}
		}



		/// <summary>
		/// Script destructor. Clean up any objects created to prevent memory leaks.
		/// </summary>
		private void OnShutdown(object sender, EventArgs e)
		{
			clearAllSectorCheckpoints();
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
				// TODO: exitRaceMode()
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
					//raceMode = true;
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
			if (chkpt.marker.blip != null) chkpt.marker.blip.Delete();
			if (chkpt.marker.checkpoint != null) chkpt.marker.checkpoint.Delete();

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
		private void enterRaceMode(bool verbose = true)
		{
			// set the 2nd SectorCheckpoint as active (there must be at least 2 SectorCheckpoints to start race mode); draw the checkpoint
			//activeCheckpoint = markedSectorCheckpoints[1];
			activateRaceCheckpoint(1);

			// teleport player to the starting checkpoint; set player orientation
			Game.Player.Character.CurrentVehicle.Position = activeCheckpoint.position;
			Game.Player.Character.CurrentVehicle.Quaternion = activeCheckpoint.quarternion;

			if (verbose)
				GTA.UI.Screen.ShowSubtitle("Lap Timer: Starting race...");
		}



		/// <summary>
		/// Activate the provided SectorCheckpoint after deactivating the current active checkpoint. 
		/// By activating, a marker will be placed at the checkpoint, and timer will run until player is in range of the checkpoint.
		/// </summary>
		/// <param name="idx">List index of SectorCheckpoint to activate in <c>markedSectorCheckpoints</c></param>
		/// <returns>The now-active SectorCheckpoint</returns>
		private SectorCheckpoint activateRaceCheckpoint(int idx)
		{
			// deactivate current active checkpoint's marker
			//hideMarker(markedSectorCheckpoints[activeSector]);

			// set the new SectorCheckpoint as active (by index)
			activeSector = idx;
			activeCheckpoint = markedSectorCheckpoints[idx];
			bool isFinal = idx == markedSectorCheckpoints.Count;			// determine if this is the final checkpoint based on the index

			// the marker placed should be different, depending on whether this checkpoint is final
			if (isFinal)
				placeMarker(activeCheckpoint.position, MarkerType.raceFinish, idx);

			// if not final checkpoint, place a checkpoint w/ an arrow pointing to the next checkpoint
			else
			{
				Vector3 nextChkptPosition = markedSectorCheckpoints[idx + 1].position;
				placeMarker(activeCheckpoint.position, MarkerType.raceArrow, idx, checkpointRadius, nextChkptPosition);
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
				GTA.UI.Notification.Show("~r~Lap Timer: You must place at 2 checkpoints in Placement Mode before entering Race Mode.");
				return false;
			}

			// if all criteria passed, checkpoints are valid
			return true;
		}

		#endregion
	}


	#region structs
	class SectorCheckpoint
	{
		// placement data
		public Vector3 position;			// Entity.Position
		public Quaternion quarternion;		// Entity.Quarternion
		public Marker marker;
		public int number;

		// race data - all times are tracked as milliseconds
		public int recordSectorTime =	int.MaxValue;
		public int bestSectorTime =		int.MaxValue;
		public int lastSectorTime;
		public int timesCompleted = 0;
	}

	struct Marker
	{
		public Blip blip;
		public Checkpoint checkpoint;
		public bool active;
	}

	enum MarkerType {
		placement,
		raceArrow,
		raceFinish,
		raceAirArrow,
		raceAirFinish,
	}
	#endregion

}

namespace LapTimer
{
	public class Class1
	{
		// Nothing goes here
	}
}
// Useful Links
// All Vehicles - https://pastebin.com/uTxZnhaN
// All Player Models - https://pastebin.com/i5c1zA0W
// All Weapons - https://pastebin.com/M3kD9pnJ
// GTA V ScriptHook V Dot Net - https://www.gta5-mods.com/tools/scripthookv-net