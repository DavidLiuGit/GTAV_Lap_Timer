using System;
using System.Collections.Generic;

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
			UIMenu raceImportMenu = _menuPool.AddSubMenu(mainMenu, "Race Import Menu");
			buildRaceImportMenu(raceImportMenu);

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



		private UIMenu buildRaceImportMenu(UIMenu submenu)
		{
			// get a List all races that can be imported
			List<ImportableRace> races = RaceExporter.getImportableRaces();
			
			// iterate over each race & add to menu, along with their handlers
			foreach (ImportableRace r in races){
				UIMenuItem item = new UIMenuItem(r.name, r.version ?? "v1.x");
				item.Activated += (menu, sender) =>
				{
					race.importRace(r.filePath);
					_menuPool.CloseAllMenus();
				};
				submenu.AddItem(item);
			}

			return submenu;
		}

		#endregion

	}




}
