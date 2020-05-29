using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.Math;

using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;


namespace LapTimer
{
	class RaceExporter
	{
		protected const string rootPath = "./scripts/LapTimer/";
		protected const string fileExt = ".json";
		protected const string scriptVersion = "v3.0";
		
		/// <summary>
		/// Create an instance of <c>ExportableRace</c> with data provided.
		/// </summary>
		/// <param name="name">Name of race</param>
		/// <param name="chkpts">List of SectorCheckpoints</param>
		/// <param name="lapMode">Whether the race should run in lap mode</param>
		/// <returns></returns>
		public static ExportableRace createExportableRace (string name, List<SectorCheckpoint> chkpts, bool lapMode){
			// create new instance of ExportableRace & set metadata
			ExportableRace race = new ExportableRace();
			race.name = name;
			race.lapMode = lapMode;
			race.numCheckpoints = chkpts.Count;
			race.version = scriptVersion;

			// iterate over list of SectorCheckpoints and simplify each before adding to ExportableRace
			race.checkpoints = new SimplifiedCheckpoint[chkpts.Count];
			for (int i = 0; i < chkpts.Count; i++)
			{
				SimplifiedCheckpoint sc = new SimplifiedCheckpoint();
				sc.position = chkpts[i].position;
				sc.quarternion = chkpts[i].quarternion;
				sc.number = chkpts[i].number;
				race.checkpoints[i] = sc;
			}

			return race;
		}



		/// <summary>
		/// Serialize and write given object to JSON.
		/// </summary>
		/// <param name="obj">Object to serialize</param>
		/// <param name="fileName">Name of file (without extension)</param>
		/// <returns></returns>
		public static string serializeToJson(object obj, string fileName)
		{
			// create output filestream
			fileName = getFilePath(fileName);
			System.IO.FileStream file = System.IO.File.Create(rootPath + fileName);

			// instantiate JSON serializer
			var serializer = new DataContractJsonSerializer(obj.GetType());
			serializer.WriteObject(file, obj);

			// close file stream & return file name
			file.Close();
			return fileName;
		}
		


		/// <summary>
		/// Deserialize <c>ExportableRace</c> from a JSON file
		/// </summary>
		/// <param name="fileName">name of JSON file to read from</param>
		/// <returns></returns>
		public static ExportableRace deserializeFromJson (string fileName, bool exactPath = false)
		{
			try {
				// attempt to open the file for reading
				fileName = getFilePath(fileName);
				string filePath = exactPath ? fileName : rootPath + fileName;	// if exactPath is false, then prepend rootPath
				System.IO.FileStream file = System.IO.File.OpenRead(filePath);
				
				// instantiate JSON deserializer
				var deserializer = new DataContractJsonSerializer(typeof(ExportableRace));
				return (ExportableRace) deserializer.ReadObject(file);
			}
			catch {
				GTA.UI.Screen.ShowSubtitle("~r~Lap Timer: failed to load race.");
				throw;
			}
		}

		


		/// <summary>
		/// Get a List of <c>ImportableRace</c> from the default script output directory.
		/// </summary>
		/// <returns>List of <c>ImportableRace</c></returns>
		public static List<ImportableRace> getImportableRaces()
		{
			// get all .json files in the script directory
			string[] files = Directory.GetFiles(rootPath, "*.json");

			// instantiate list of importable races
			List<ImportableRace> races = new List<ImportableRace>();

			// attempt to deserialize each file to ImportableRace
			foreach (string fileName in files)
			{
				try
				{
					// attempt to deserialize to ImportableRace
					System.IO.FileStream fs = System.IO.File.OpenRead(fileName);
					DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(ImportableRace));
					ImportableRace race = (ImportableRace)deserializer.ReadObject(fs);

					// validate the ImportableRace instance; add to races if valid
					race.filePath = fileName;
					races.Add(race);
				}
				catch { throw; }

			}

			return races;
		}



		protected static string getFilePath(string fileName)
		{
			if (!fileName.EndsWith(fileExt)) fileName += fileExt;			// append file extension, if it is not there already
			return fileName;
		}
	}



	class TimingSheetExporter : RaceExporter
	{
		new protected const string rootPath = "./scripts/LapTimer/timing_sheets/";


		public static string serializeToJson(ExportableTimingSheet obj, string fileName)
		{
			// create output filestream
			fileName = getFilePath(fileName);
			System.IO.FileStream file = System.IO.File.Create(rootPath + fileName);

			// instantiate JSON serializer
			var serializer = new DataContractJsonSerializer(typeof(ExportableTimingSheet));
			serializer.WriteObject(file, obj);

			// close file stream & return file name
			file.Close();
			return fileName;
		}



		/// <summary>
		/// Deserialize <c>ExportableTimingSheet</c> from a JSON file
		/// </summary>
		/// <param name="fileName">name of JSON file to read from</param>
		/// <returns></returns>
		new public static ExportableTimingSheet deserializeFromJson(string fileName, bool exactPath = false)
		{
			try
			{
				// attempt to open the file for reading
				fileName = getFilePath(fileName);
				string filePath = exactPath ? fileName : rootPath + fileName;	// if exactPath is false, then prepend rootPath
				System.IO.FileStream file = System.IO.File.OpenRead(filePath);

				// instantiate JSON deserializer
				var deserializer = new DataContractJsonSerializer(typeof(ExportableTimingSheet));
				return (ExportableTimingSheet)deserializer.ReadObject(file);
			}
			catch
			{
				throw;
			}
		}


	}



	public struct ImportableRace
	{
		public string version;
		public string name;
		public string description;
		public bool lapMode;

		public string filePath;
	}


	public struct ExportableRace
	{
		// metadata
		public string version;	// script version that the race was exported from/intended for
		public string name;		// name of the race
		public string description;

		public bool lapMode;
		public int numCheckpoints;
		public SimplifiedCheckpoint[] checkpoints;
	}


	public struct SimplifiedCheckpoint
	{
		public Vector3 position;
		public Quaternion quarternion;
		public int number;
	}


	public struct ExportableTimingSheet
	{
		public DateTime exportDatetime;
		public SimplifiedCheckpointTimingData[] timingData;
		public int raceHashCode;
	}


	public struct SimplifiedCheckpointTimingData
	{
		public int fastestTime;
		public Dictionary<string, int> vehicleFastestTime;
		public int checkpointHashcode;
	}
}
