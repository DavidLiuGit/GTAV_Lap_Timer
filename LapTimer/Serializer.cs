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

namespace LapTimer
{
	class RaceExporter
	{
		static string rootPath = "./scripts/LapTimer/";
		static string fileExt = ".json";
		
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
		public static string serializeToJson(ExportableRace obj, string fileName)
		{
			// create output filestream
			if (!fileName.EndsWith(fileExt)) fileName += fileExt;			// append file extension, if it is not there already
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
		public static ExportableRace deserializeFromJson (string fileName)
		{
			try {
				// attempt to open the file for reading
				if (!fileName.EndsWith(fileExt)) fileName += fileExt;			// append file extension, if it is not there already
				System.IO.FileStream file = System.IO.File.OpenRead(rootPath + fileName);
				
				// instantiate JSON deserializer
				var deserializer = new DataContractJsonSerializer(typeof(ExportableRace));
				return (ExportableRace) deserializer.ReadObject(file);
			}
			catch {
				GTA.UI.Screen.ShowSubtitle("~r~Lap Timer: failed to load race - file not found.");
				throw;
			}
		}
	}



	public struct ExportableRace
	{
		public string name;		// name of the race
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
}
