using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {/*
 *   R e a d m e
 *   -----------
 *   This script can be used as-is, and it is simple to understand and modify. Simply upload it to a Programmable block and compile. (No timer block needed)
 *   It automatically closes all the doors on the local grid, after a specified period (default 3 secs) has elapsed from the moment the door was opened.
 *   
 *   If you want to increase / decrease the close delay, pass a 2nd parameter of type double, to the constructor of the 'DoorManager' class (or just change the hard-coded value). The default is 3 seconds. Eg: `doorManager = new DoorManager(this,0.5);` This will set the close delay to half a seconds.
 *   If you want the script to auto-close doors on connected grids as well, then pass a 3rd parameter of type bool, to the constructor of the 'DoorManager' class (or just change the hard-coded value). The default is false. Eg: `doorManager = new DoorManager(this,0.5,true);` This will also auto-close doors on connected grids.
 *   The bulk of this script was put into a class so that it can be easily combined with existing scripts.
 *   
 *   And that is all there is to it. Enjoy your auto-closing doors!
 *   
 *   Author: Stuyvenstein
 *   
 *   Thanks to malware-dev for the awesome MDK!
 *   
 */
        private DoorManager doorManager;

        public Program()
        {
            doorManager = new DoorManager(this);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {

        }

        public void Main(string argument)
        {
            doorManager?.Run();
        }

        public class DoorManager
        {
            private List<AutoDoor> AutoDoors = new List<AutoDoor>();
            private int _CloseDelayMilliSeconds;
            private bool _AffectConnectedGrids;
            private Program ParentProgram;
            private int ElapsedTicks = 0;
            private string lcdNAME = "Server (AutoDoors)";


            public DoorManager(Program callingProgram, double CloseDelaySeconds = 3, bool AffectConnectedGrids = false)
            {
                _CloseDelayMilliSeconds = (int)(CloseDelaySeconds * (double)1000);
                _AffectConnectedGrids = AffectConnectedGrids;
                ParentProgram = callingProgram;
                UpdateDoorList();
            }

            //This function maintains the list of doors to be auto-closed, and executes every 300 ticks
            private void UpdateDoorList()
            {
                List<IMyDoor> allGridDoors = new List<IMyDoor>();
                //Gets all the doors on the local, and all the connected grids
                if (_AffectConnectedGrids) ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyDoor>(allGridDoors, d => !d.CustomData.ToLower().Contains("-ignore"));
                //Gets all the doors on the current grid only
                else ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyDoor>(allGridDoors, d => (d.CubeGrid == ParentProgram.Me.CubeGrid && !d.CustomData.ToLower().Contains("-ignore")));

                //Repopulate doors
                AutoDoors.Clear();
                foreach (IMyDoor gridDoor in allGridDoors)
                {
                    AutoDoor autoDoor = new AutoDoor();
                    autoDoor.doorRef = gridDoor;
                    AutoDoors.Add(autoDoor);
                }
            }

            public void Run()
            {
                int doorsClosed = 0;
                foreach (AutoDoor autoDoor in AutoDoors)
                {
                    //Find open doors that aren't yet flagged for auto-closing
                    if (autoDoor.doorRef?.Status == (DoorStatus.Open | DoorStatus.Opening) && !autoDoor.IsTiming && autoDoor.doorRef.CustomData.Contains("Hangar"))
                    {
                        //Flag door for auto-closing
                        autoDoor.IsTiming = true;
                        autoDoor.TimeOpened = DateTime.Now;
                    }

                    //Check if opened door has reached or passed the door close delay period, and closes it if true
                    if (autoDoor.doorRef?.Status == (DoorStatus.Open | DoorStatus.Opening) && autoDoor.IsTiming)
                    {
                        if (DateTime.Now.Subtract(autoDoor.TimeOpened).TotalMilliseconds >= _CloseDelayMilliSeconds)
                        {
                            autoDoor.doorRef.CloseDoor();
                            autoDoor.IsTiming = false;
                            doorsClosed++;
                        }
                    }

                    //Handle manually closed doors
                    if (autoDoor.doorRef?.Status == (DoorStatus.Closed | DoorStatus.Closing) && autoDoor.IsTiming) autoDoor.IsTiming = false;
                }

                IMyTextSurface mesurface0 = ParentProgram.Me.GetSurface(0);
                mesurface0.ContentType = ContentType.TEXT_AND_IMAGE;
                mesurface0.FontSize = 2;
                mesurface0.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                if (doorsClosed != 0)
                {
                    mesurface0.WriteText("Doors Closed: " + doorsClosed.ToString());
                }

                //IMyTextPanel thisLCD = (IMyTextPanel)ParentProgram.GridTerminalSystem.GetBlockWithName(lcdNAME);
                //try
                //{
                    //thisLCD.ContentType = ContentType.TEXT_AND_IMAGE;
                    //thisLCD.FontSize = 16;
                    //thisLCD.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                    //thisLCD.WriteText("Doors Closed: " + doorsClosed.ToString());
                //}
                //catch
                //{
                //    ("LCD NAME INCORRECT");
                //}

                //This script runs every 10 ticks, and we want to update the door list every 300 ticks, hence the below
                if (ElapsedTicks == 30)
                {
                    UpdateDoorList();
                    ElapsedTicks = 0;
                }
                else
                {
                    ElapsedTicks++;
                }
            }

            public class AutoDoor
            {
                public IMyDoor doorRef;
                public bool IsTiming = false;
                public DateTime TimeOpened;
            }

        }
    }
}
