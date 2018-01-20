using UnityEngine;
using System.IO;
using System;
using KSP_World_Nav;

namespace KSP_VORDME
{
    public class VORDME : PartModule
    {
        private VOR cVOR = null;
        private DME cDME = null;
        private NAVmaster cNav_Master = new NAVmaster();

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Broadcast"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
        public bool bBroadcast;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Frequency", guiFormat = "F2")]
        public double dFrequency = 108.00;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Up")]
        public void nextFrequency()
        {
            if (cVOR != null)
            {
                cVOR.incrementChannel();
                dFrequency = cVOR.getFrequency();
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down")]
        public void prevFrequency()
        {
            if (cVOR != null)
            {
                cVOR.decrementChannel();
                dFrequency = cVOR.getFrequency();
            }
        }
    


    //        private string[] sModeDescription = { "Low", "Terminal", "High" };
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Mode"), UI_ChooseOption(options = new string[] { "L", "T", "H" })]
        public string sMode = "L";

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Bearing Offset"), UI_FloatRange(maxValue = 180.0f, minValue = -180.0f, stepIncrement = 0.05f)]
        public float fBearing_Offset = 0.0f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Station ID")]
        public string sStation_ID = "KSP";

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Up")]
        public void nextID1()
        {
            if (cVOR != null)
            {
                cVOR.incrementStationID(0);
                sStation_ID = cVOR.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Down")]
        public void prevID1()
        {
            if (cVOR != null)
            {
                cVOR.decrementStationID(0);
                sStation_ID = cVOR.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Up")]
        public void nextID2()
        {
            if (cVOR != null)
            {
                cVOR.incrementStationID(1);
                sStation_ID = cVOR.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Down")]
        public void prevID2()
        {
            if (cVOR != null)
            {
                cVOR.decrementStationID(1);
                sStation_ID = cVOR.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(3) Select Up")]
        public void nextID3()
        {
            if (cVOR != null)
            {
                cVOR.incrementStationID(2);
                sStation_ID = cVOR.sStation_ID;
            }
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(3) Select Down")]
        public void prevID3()
        {
            if (cVOR != null)
            {
                cVOR.decrementStationID(2);
                sStation_ID = cVOR.sStation_ID;
            }
        }

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Latitude")]
        public string sLatitude;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Longitude")]
        public string sLongitude;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Elevation", guiFormat = "F1")]
        public double dElevation;

        // Estimating from the Z-400 battery, which weights 20 kg (0.02 t) and has a charge of 400, and a "typical" Li battery that stores about 18 kJ in 20g, 
        // the battery contains 18 MJ, meaning 1 charge unit is about 45 kJ. This leads to charge demand 1 = 45 kW, or 0.00060 = 27 W, typical for a commercial aircraft ADU
        private double dW_to_EC = 1.0 / 45000.0;
        private double ElectricPowerRequired;

        public override void OnActive()
        {
        }
        private int iStation_ID = -1;
        private void initialize()
        {
            if (cVOR == null)
            {
                cVOR = new VOR();
                cVOR.iStation_ID_Characters = new int[3];
                cVOR.iStation_ID_Characters[0] = 10; // K
                cVOR.iStation_ID_Characters[1] = 18; // S
                cVOR.iStation_ID_Characters[2] = 15; // P
                cVOR.buildStationID();
                sStation_ID = cVOR.sStation_ID;
            }
            if (cNav_Master == null)
                cNav_Master = new NAVmaster();
        }
        private void register()
        {
            initialize();
            if (iStation_ID == -1)
            {
                //print("VOR station register");
                iStation_ID = cNav_Master.registerStation(cVOR);
            }
        }
        public override void OnInitialize()
        {
            base.OnInitialize();
            //print("VOR initialize");
            initialize();
        }
        public override void OnAwake()
        {
            base.OnAwake();
            //print("VOR awake");
            initialize();
        }
        public override void OnInactive()
        {
            base.OnInactive();
            //print("VOR inactive");
        }
        public override void OnCopy(PartModule fromModule)
        {
            base.OnCopy(fromModule);
            //print("VOR copy");
        }

        public override void OnStart(StartState state)
        {
            register();
            cVOR.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude);
            cNav_Master.updateStation(iStation_ID, cVOR);
            base.OnStart(state);
            //print("VOR start");
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            cVOR.OnSave(node);
        }
        public override void OnLoad(ConfigNode node)
        {
            initialize();
            //print("VOR load");
            base.OnLoad(node);
            cVOR.OnLoad(node);
            if (iStation_ID != -1)
                cNav_Master.updateStation(iStation_ID, cVOR);
            sStation_ID = cVOR.sStation_ID;
            dFrequency = cVOR.getFrequency();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            initialize(); // make sure that everything is registered
            //string ResName = "ElectricCharge";

            // radio broadcast power
            double dBroadcast_Power = 0.0;
            if (sMode == "L")
                dBroadcast_Power = 80.0;
            else if (sMode == "T")
                dBroadcast_Power = 50.0;
            else// if (sMode == "H")
                dBroadcast_Power = 260.0;
            ElectricPowerRequired = dBroadcast_Power * dW_to_EC;
            ElectricPowerRequired += 100.0 * dW_to_EC; // electronics power - maybe exaggerated

            double dElectric_Draw = ElectricPowerRequired * TimeWarp.deltaTime;
            double elecAvail = 0.0;
            if (bBroadcast)
                elecAvail = part.RequestResource("ElectricCharge", dElectric_Draw) / dElectric_Draw;
            if (elecAvail < 0.90)
                dBroadcast_Power = 0.0;
            //string sStr1 = "Broadcast Power " + dBroadcast_Power.ToString("0.0");
            //print(sStr1);
            bool bUpdate_Flag = false;
            if (dBroadcast_Power != cVOR.dTransmit_Power)
            {
                cVOR.dTransmit_Power = dBroadcast_Power;
                bUpdate_Flag = true;
            }
            if (cVOR.dRadial_Offset != (double)fBearing_Offset)
            {
                cVOR.dRadial_Offset = (double)fBearing_Offset;
                bUpdate_Flag = true;
            }

            //string sStr2 = "Update required pre position " + bUpdate_Flag;
            //print(sStr2);

            if (cVOR.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude))
                bUpdate_Flag = true;

            //string sStr3 = "Update required post position " + bUpdate_Flag;
            //print(sStr3);

            sLatitude = cVOR.vPosition.sLatitude_Human;
            sLongitude = cVOR.vPosition.sLongitude_Human;
            dElevation = vessel.altitude;

            if (bUpdate_Flag)
                cNav_Master.updateStation(iStation_ID, cVOR);

            //string sStr4 = "Update complete ";
            //print(sStr4);
        }
    }
}
