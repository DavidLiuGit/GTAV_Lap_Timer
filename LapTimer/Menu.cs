using System;
using System.Collections.Generic;

using GTA;
using NativeUI;


namespace LapTimer
{
	class NativeUIMenu
	{
		List<SectorCheckpoint> sc;

		public MenuPool _menuPool;
		public UIMenu mainMenu;

		public NativeUIMenu(ref List<SectorCheckpoint> checkpoints)
		{
			sc = checkpoints;

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
