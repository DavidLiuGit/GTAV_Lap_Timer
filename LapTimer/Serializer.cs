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
		static string jsonExt = ".json";

		/// <summary>
		/// Serialize a List of <c>SectorCheckpoint</c> and write it to an XML file with the specified prefix
		/// </summary>
		/// <param name="obj">SectorCheckpoints to serialize</param>
		/// <param name="name">Name of file to write out to</param>
		public static void writeXML(object obj, string name){
			// create output file
			System.IO.FileStream file = System.IO.File.Create(rootPath + name + ".xml");
			
			// create XML writer and write to FileStream
			System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(obj.GetType());
			writer.Serialize(file, obj);
			file.Close();
		}


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
				race.checkpoints[i] = sc;
			}

			return race;
		}


		public static void writeToJson(object obj, string fileName)
		{
			// create output filestream
			System.IO.FileStream file = System.IO.File.Create(rootPath + fileName + jsonExt);

			// instantiate JSON serializer
			var serializer = new DataContractJsonSerializer(obj.GetType());
			serializer.WriteObject(file, obj);

			file.Close();
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
	}
}
