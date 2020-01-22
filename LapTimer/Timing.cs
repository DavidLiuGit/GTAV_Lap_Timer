using System;
using System.Collections.Generic;


namespace LapTimer
{
	public enum TimeType
	{
		Regular = 'w',
		VehicleBest = 'g',		// best time achieved for the current vehicle
		Record = 'p',		// best time achieved
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
		private void setLatestSplitTimes()
		{
			// compute & set record split time
			latestRecordSplitTime = latestTime - fastestTime;

			// compute & set vehicle split time, if possible
			if (vehicleFastestTime.ContainsKey(latestVehicle))
				latestVehicleSplitTime = latestTime - vehicleFastestTime[latestVehicle];
			else latestVehicleSplitTime = 0;
		}
	}
}
