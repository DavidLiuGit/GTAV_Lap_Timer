using System;
using System.Collections.Generic;
using System.Linq;

using GTA;
using NativeUI;


namespace LapTimer
{
	class NativeUIMenu
	{
		RaceControl race;

		public MenuPool _menuPool;
		public UIMenu mainMenu;

		public NativeUIMenu(ref RaceControl raceControl)
		{
			race = raceControl;

			// create new menu pool & add the main menu to it
			_menuPool = new MenuPool();
			_menuPool.Add(buildMainMenu());
		}


		#region publicMethods

		/// <summary>
		/// Toggle visibility of menus. If no menu is currently open, open main menu.
		/// </summary>
		/// <returns>boolean indicating whether a menu is now open</returns>
		public bool toggleMenu()
		{
			if (_menuPool.IsAnyMenuOpen())
			{
				_menuPool.CloseAllMenus();
				return false;
			}
			else
			{
				mainMenu.Visible = true;
				return true;
			}
		}

		#endregion



		#region menus

		private UIMenu buildMainMenu()
		{
			mainMenu = new UIMenu("Race Timer", "~b~by iLike2Teabag");

			// add a submenu to handle race imports
			UIMenu raceImportMenu = _menuPool.AddSubMenu(mainMenu, "Race Import Menu", "Choose races to import from file");
			buildRaceImportMenu(raceImportMenu);

			// add a submenu for race control
			UIMenu raceControlMenu = _menuPool.AddSubMenu(mainMenu, "Race Control Menu", "Modify checkpoints and race mode");
			raceControlMenu.OnMenuOpen += buildRaceControlMenu;
			//buildRaceControlMenu(raceControlMenu);

			// add a submenu for Timing Sheet
			UIMenu lapTimeMenu = _menuPool.AddSubMenu(mainMenu, "Lap Times", "Display lap times for the current race");
			lapTimeMenu.OnMenuOpen += loadLapTimeMenu;

			// add controls to enter placement & race modes
			UIMenuItem placementToggle = new UIMenuItem("Toggle Placement Mode");
			UIMenuItem raceToggle = new UIMenuItem("Toggle Race Mode");
			placementToggle.Activated += (menu, sender) => race.togglePlacementMode();
			raceToggle.Activated += (menu, sender) => race.toggleRaceMode();
			mainMenu.AddItem(placementToggle);
			mainMenu.AddItem(raceToggle);

			// add control to export race
			UIMenuItem exportRaceItem = new UIMenuItem("Export Race");
			exportRaceItem.Activated += (menu, sender) => race.exportRace();
			mainMenu.AddItem(exportRaceItem);

			mainMenu.RefreshIndex();
			return mainMenu;
		}
		


		private void loadLapTimeMenu(UIMenu sender)
		{
			// clear the menu
			sender.Clear();

			// validate the race; if race is invalid
			if (!race.isValid)
				return;

			// get the last checkpoint in list of checkpoints
			SectorCheckpoint finalChkpt = race.finishCheckpoint;

			// iterate over each k-v in the final checkpoint's timing data
			var times = finalChkpt.timing.vehicleFastestTime.OrderBy(x => x.Value);
			foreach (KeyValuePair<string, int> entry in times)
			{
				sender.AddItem(new UIMenuItem(TimingData.msToReadable(entry.Value, false, true) + " - " + entry.Key));
			}

			sender.RefreshIndex();
		}


		private UIMenu buildRaceImportMenu(UIMenu submenu)
		{
			// get a List all races that can be imported
			List<ImportableRace> races = RaceExporter.getImportableRaces();
			
			// iterate over each race & add to menu, along with their handlers
			foreach (ImportableRace r in races){
				string descriptionString = r.name + 
					"\nMode: " + (r.lapMode ? "circuit" : "point-to-point") + 
					"\nVersion: " + r.version ?? "v1.x";
				UIMenuItem item = new UIMenuItem(r.name, descriptionString);
				item.Activated += (menu, sender) =>
				{
					race.importRace(r.filePath);
					_menuPool.CloseAllMenus();
				};
				submenu.AddItem(item);
			}

			return submenu;
		}



		private void buildRaceControlMenu(UIMenu submenu)
		{
			submenu.Clear();

			// add checkbox to toggle lap mode
			string lapModeDescription = "If checked, race is a circuit, and automatically restarts. If unchecked, race is point-to-point";
			UIMenuCheckboxItem lapModeItem = new UIMenuCheckboxItem("Lap Mode", race.lapRace, lapModeDescription);
			lapModeItem.CheckboxEvent += (sender, status) => race.lapRace = status;
			submenu.AddItem(lapModeItem);

			// add button to place checkpoint
			UIMenuItem addCheckpointBtn = new UIMenuItem("Place checkpoint", "Place a checkpoint at the player's current location");
			addCheckpointBtn.Activated += (m, i) => race.createSectorCheckpoint();
			submenu.AddItem(addCheckpointBtn);

			// undo last placed checkpoint
			UIMenuItem undoCheckpointBtn = new UIMenuItem("Undo last checkpoint", "Remove the last checkpoint");
			undoCheckpointBtn.Activated += (m, i) => race.deleteLastSectorCheckpoint();
			submenu.AddItem(undoCheckpointBtn);

			// delete all checkpoints
			UIMenuItem deleteAllCheckpointsBtn = new UIMenuItem("Delete all checkpoints");
			deleteAllCheckpointsBtn.Activated += (m, i) => race.clearAllSectorCheckpoints();
			submenu.AddItem(deleteAllCheckpointsBtn);

			//return submenu;
		}

		#endregion

	}




}
