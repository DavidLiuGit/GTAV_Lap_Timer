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


		public UIMenu buildMainMenu()
		{
			mainMenu = new UIMenu("Race Timer", "~b~by iLike2Teabag");

			mainMenu.RefreshIndex();
			return mainMenu;
		}

	}
}
