using System;
using System.Drawing;

using GTA;
using GTA.Native;
using GTA.Math;

namespace LapTimer
{
	public class SectorCheckpoint
	{
		// defaults
		private Color defaultColor = Color.FromArgb(255, 255, 66);

		// placement data
		public Vector3 position;			// Entity.Position
		public Quaternion quarternion;		// Entity.Quarternion
		public Marker marker;
		public int number;

		// placement constants
		public const float checkpointRadius = 8.0f;
		private readonly Vector3 checkpointOffset = new Vector3(0.0f, 0.0f, -1.0f);	// visually, the checkpoint will be offset by this vector

		// race data - all times are tracked as milliseconds
		public TimingData timing = new TimingData();



		public SectorCheckpoint(int _number)
			: this(_number, Game.Player.Character.Position, Game.Player.Character.Quaternion)
		{ }

		public SectorCheckpoint(int _number, Vector3 pos, Quaternion quat, bool verbose = true)
		{
			// assign metadata
			number = _number;
			position = pos;
			quarternion = quat;

			// place a marker (placement mode)
			marker = placeMarker(MarkerType.placement, number);

			// debug printout if verbose
			if (verbose)
				GTA.UI.Notification.Show("Lap Timer: created checkpoint #" + number);
		}



		/// <summary>
		/// Place a marker (pair of blip and checkpoint) at the current checkpoint's position.
		/// </summary>
		/// <param name="type">Indicate whether checkpoint is to be used in a race or in placement mode</param>
		/// <param name="number">Placement mode only: the number to display on the checkpoint</param>
		/// <param name="radius">Radius of the checkpoint, in meters</param>
		/// <param name="target">Race mode only: position of the next checkpoint, if applicable. Omit or pass in <c>null</c> if not applicable</param>
		public Marker placeMarker(MarkerType type, int number = 0, Vector3? target = null, float radius = checkpointRadius)
		{
			// if the current instance of marker is already active, do nothing and return the instance
			if (marker.active == true)
				return marker;

			// instantiate empty Marker
			Marker newMarker = new Marker();

			// place a placement mode checkpoint
			if (type == MarkerType.placement)
			{
				newMarker.checkpoint = GTA.World.CreateCheckpoint(
									new GTA.CheckpointCustomIcon(CheckpointCustomIconStyle.Number, Convert.ToByte(number)),
									position + checkpointOffset, position + checkpointOffset,
									radius, defaultColor);
			}

			// place a regular race checkpoint
			else if (type == MarkerType.raceArrow || type == MarkerType.raceFinish)
			{
				if (type == MarkerType.raceArrow)
					newMarker.checkpoint = GTA.World.CreateCheckpoint(CheckpointIcon.CylinderDoubleArrow,
						position + checkpointOffset, checkpointOffset + target ?? new Vector3(0, 0, 0), radius, defaultColor);
				else if (type == MarkerType.raceFinish)
					newMarker.checkpoint = GTA.World.CreateCheckpoint(CheckpointIcon.CylinderCheckerboard,
						position + checkpointOffset, position + checkpointOffset, radius, defaultColor);
			}

			// create blip
			newMarker.blip = GTA.World.CreateBlip(position);
			newMarker.blip.NumberLabel = number;

			// flag the marker as active and return this instance of Marker
			newMarker.active = true;
			return newMarker;
		}



		/// <summary>
		/// Clear active marker of this checkpoint
		/// </summary>
		public void hideMarker()
		{
			if (marker.active)
			{
				marker.checkpoint.Delete();
				marker.blip.Delete();
			}
			marker.active = false;
		}
	}





	public struct Marker
	{
		public Blip blip;
		public Checkpoint checkpoint;
		public bool active;
	}

	public enum MarkerType
	{
		placement,
		raceArrow,
		raceFinish,
		raceAirArrow,
		raceAirFinish,
	}
}
