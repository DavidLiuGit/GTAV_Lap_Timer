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

namespace LapTimer // !!!! IMPORTANT REPLACE THIS WITH YOUR MODS NAME !!!!
{
    public class Main : Script
    {
        // You can set your mod information below! Be sure to do this!
        bool firstTime = true;
        string ModName = "Lap Timer";
        string Developer = "iLike2Teabag";
        string Version = "1.0";

        public Main()
        {
            Tick += onTick;
            KeyDown += onKeyDown;
            Interval = 1;
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

        // placement mode variables
        List<SectorCheckpoint> markedSectorCheckpoints = new List<SectorCheckpoint>();                // add to this list when player marks a position



        // ------------- KEY PRESS EVENT LISTENER -----------------
        private void onKeyDown(object sender, KeyEventArgs e)
        {
            // enter/exit placement mode with F5
            if (e.KeyCode == Keys.F5)
            {
                togglePlacementMode();
            }

            // if placement mode is enabled, and the control key was used:
            if (placementMode && e.Modifiers == Keys.Control)
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
                        clearAllCheckpoints();
                        break;
                }
            }
        }



        // ------------- HELPER METHODS -----------------
        #region helpers

        /// <summary>
        /// Enter/exit "placement mode", which allows user to mark positions as checkpoints in a lap. Can only be entered if raceMode=false
        /// </summary>
        private void togglePlacementMode()
        {
            if (raceMode){
                GTA.UI.Screen.ShowSubtitle("~r~Lap Timer: Cannot enter placement mode while race mode is active.");
                return;
            }

            // if entering placement mode
            if (!placementMode) 
            {
                placementMode = true;
                GTA.UI.Screen.ShowSubtitle("Lap Timer: Entering placement mode. Mark sector checkpoints using Ctrl+X.");
            }
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
            Vector3 playerPos = Game.Player.Character.Position;
            playerPos.Z -= 1.0f;
            newCheckpoint.position = playerPos;

            // assign SectorCheckpoint number, based on length of markedSectorCheckpoints list.
            int checkpointNum = markedSectorCheckpoints.Count;
            newCheckpoint.number = checkpointNum;

            // place marker (blip + checkpoint)
            newCheckpoint.marker = placeMarker(playerPos, MarkerType.placement, checkpointNum);

            // print output if verbose
            if (verbose)
                GTA.UI.Screen.ShowSubtitle("Lap Timer: placed checkpoint #" + checkpointNum);

            return newCheckpoint;
        }



        /// <summary>
        /// Place a marker (pair of blip and checkpoint) at the specified position.
        /// </summary>
        /// <param name="pos">Position as instance of Vector3</param>
        /// <param name="type">Indicate whether checkpoint is to be used in a race or in placement mode</param>
        private Marker placeMarker(Vector3 pos, MarkerType type, int number = 0, float radius = checkpointRadius)
        {
            // instantiate empty Marker
            Marker marker = new Marker();

            // place a placement mode marker
            if (type == MarkerType.placement)
            {
                // create checkpoint & blip
                marker.checkpoint = GTA.World.CreateCheckpoint (
                                    new GTA.CheckpointCustomIcon(CheckpointCustomIconStyle.Number, Convert.ToByte(number)),
                                    pos, pos, radius, Color.FromArgb(255, 255, 66));
                marker.blip = GTA.World.CreateBlip(pos);
            }

            return marker;
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

            // delete its Marker (Blip + Checkpoint) from the World
            chkpt.marker.blip.Delete();
            chkpt.marker.checkpoint.Delete();

            // remove the checkpoint from the list
            markedSectorCheckpoints.RemoveAt(markedSectorCheckpoints.Count - 1);

            // print output if verbose
            if (verbose)
                GTA.UI.Screen.ShowSubtitle("Lap Timer: deleted checkpoint #" + checkpointNum);
        }



        /// <summary>
        /// Clear all SectorCheckpoints, and delete all blips & checkpoints from World
        /// </summary>
        private void clearAllCheckpoints(bool verbose = true)
        {
            // clear markedSectorCheckpoints
            markedSectorCheckpoints.Clear();

            // clear World blips & checkpoints
            deleteAllWorldMarkers();

            if (verbose)
                GTA.UI.Screen.ShowSubtitle("Lap Timer: All saved SectorCheckpoints cleared. All blips & checkpoints deleted.");
        }



        /// <summary>
        /// Delete all Blips & Checkpoints from World
        /// </summary>
        private void deleteAllWorldMarkers()
        {
            // delete all blips
            Blip[] allBlips = GTA.World.GetAllBlips();
            for (int i = 0; i < allBlips.Length; i++)
                allBlips[i].Delete();

            // delete all checkpoints
            Checkpoint[] allChkpts = GTA.World.GetAllCheckpoints();
            for (int i = 0; i < allChkpts.Length; i++)
                allChkpts[i].Delete();
        }

        #endregion
    }



    struct SectorCheckpoint
    {
        public Vector3 position;
        public Marker marker;
        public int number;
    }

    struct Marker
    {
        public Blip blip;
        public Checkpoint checkpoint;
    }

    enum MarkerType {
        placement,
        raceArrow,
        raceFinish,
    }


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