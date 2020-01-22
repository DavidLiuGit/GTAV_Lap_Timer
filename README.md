# Race Timer
Also referred to as Lap Timer. This is a ScriptHookVDotNet script for GTA5. 

Test your car's performance any way you want with Race Timer. Set your own checkpoints (in Placement Mode), and take any car through the race you created.

As you drive, Race Timer shows you at each checkpoint:
- **Elapsed time**
- **Fastest split time**: delta between your current time and the best time previously achieved in any vehicle
- **Vehicle split time**: delta between your current time and best time previously achieved in the same vehicle



## Installation
Place LapTimer.dll in your `scripts` folder.
Latest ScriptHookVDotNet is required. Make sure you have `ScriptHookVDotNet3.dll` in your game's main directory.



## Usage
### Placement Mode
In this mode, you will create your custom race by placing checkpoints. Enter "Placement Mode" with F5.
- Ctrl+X: place new checkpoint
- Ctrl+Z: undo last checkpoint
- Ctrl+D: delete all checkpoints 

### Race Mode
Once you've placed at least 2 checkpoints, get in a vehicle and press F6. You will be teleported to the first checkpoint, and the timer will start. Times will be displayed at each checkpoint and at the end of the race.
- Ctrl+R: restart race 

### Colors for elapsed time
- **Purple**: overall fastest time
- **Green**: fastest time for the vehicle
- **White**: neither of the above 



## Change Log
### v1.1
- added support for INI, allowing custom hotkeys 
### v1.0
- initial release
