using UnityEngine;
using System.IO;
using System;
using KSP_ADM;
using KSP_World_Nav;

namespace KSP_ASOS
{
    public class AutomatedSufraceObservingSystem : PartModule
    {
        private AirDataModule cADM;
        private COMM cCOMM = null;
        private NAVmaster cNav_Master = new NAVmaster();

        private double dTime = 0.0;

        // Estimating from the Z-400 battery, which weights 20 kg (0.02 t) and has a charge of 400, and a "typical" Li battery that stores about 18 kJ in 20g, 
        // the battery contains 18 MJ, meaning 1 charge unit is about 45 kJ. This leads to charge demand 1 = 45 kW, or 0.00060 = 27 W, typical for a commercial aircraft ADU
        private double W_Per_Charge = 1.0 / 45000.0;
        private double ProcessorPowerRequired;
        private double BroadcastPowerRequired;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Broadcast"), UI_Toggle(disabledText = "Disabled", enabledText = "Enabled")]
        public bool bBroadcast_On = true;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Broadcast Frequency", guiFormat = "F4")]
        public double dFrequency = 121.425;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Station ID")]
        public string sStation_ID = "XKSP";

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Up")]
        public void nextFrequency()
        {
            cCOMM.incrementChannel();
            dFrequency = cCOMM.getFrequency();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down")]
        public void prevFrequency()
        {
            cCOMM.decrementChannel();
            dFrequency = cCOMM.getFrequency();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Up (1 MHz)")]
        public void nextFrequencyLarge()
        {
            cCOMM.incrementChannelLarge();
            dFrequency = cCOMM.getFrequency();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Frequency Select Down (1 MHz)")]
        public void prevFrequencyLarge()
        {
            cCOMM.decrementChannelLarge();
            dFrequency = cCOMM.getFrequency();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Up")]
        public void nextID1()
        {
            cCOMM.incrementStationID(1);
            sStation_ID = cCOMM.sStation_ID;
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(1) Select Down")]
        public void prevID1()
        {
            cCOMM.decrementStationID(1);
            sStation_ID = cCOMM.sStation_ID;
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Up")]
        public void nextID2()
        {
            cCOMM.incrementStationID(2);
            sStation_ID = cCOMM.sStation_ID;
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(2) Select Down")]
        public void prevID2()
        {
            cCOMM.decrementStationID(2);
            sStation_ID = cCOMM.sStation_ID;
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(3) Select Up")]
        public void nextID3()
        {
            cCOMM.incrementStationID(3);
            sStation_ID = cCOMM.sStation_ID;
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ID(3) Select Down")]
        public void prevID3()
        {
            cCOMM.decrementStationID(3);
            sStation_ID = cCOMM.sStation_ID;
        }

        private double dReported_Pressure_hPa;
        private double dReported_Temperature_C;
        private string sReported_METAR;
        private string sReported_Weather_English;
        private int iLastMetarUpdateTime;

        public string sCurrent_Broadcast_METAR;
        private string sCurrent_Broadcast_Weather_English;

        public string getMETAR() { return sCurrent_Broadcast_METAR; }
        public string getWeatherReport() { return sCurrent_Broadcast_Weather_English; }
        private void initialize()
        {
            if (cCOMM == null)
            {
                cCOMM = new COMM();
                cCOMM.iStation_ID_Characters = new int[4];
                cCOMM.iStation_ID_Characters[0] = 23; // X
                cCOMM.iStation_ID_Characters[1] = 10; // K
                cCOMM.iStation_ID_Characters[2] = 18; // S
                cCOMM.iStation_ID_Characters[3] = 15; // P
                cCOMM.buildStationID();
                sStation_ID = cCOMM.sStation_ID;
                dFrequency = cCOMM.getFrequency();
            }
            if (cNav_Master == null)
                cNav_Master = new NAVmaster();
            if (cADM == null)
                cADM = new AirDataModule();
            ProcessorPowerRequired = 50.0 * W_Per_Charge;
            BroadcastPowerRequired = 10.0 * W_Per_Charge;
            cADM.initialize();
            iLastMetarUpdateTime = -1;
            dTime = 0.0;
        }
        int iStation_ID = -1;
        private void register()
        {
            initialize();
            if (iStation_ID == -1)
            {
                //print("ASOS station register");
                iStation_ID = cNav_Master.registerStation(cCOMM);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (cCOMM != null)
                cCOMM.OnLoad(node);
            sStation_ID = cCOMM.sStation_ID;
            dFrequency = cCOMM.getFrequency();
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (cCOMM != null)
                cCOMM.OnSave(node);
        }

        public override void OnActive()
        {
            initialize();
        }
        public override void OnAwake()
        {
            initialize();
        }
        public override void OnInactive()
        {
        }

        public override void OnStart(StartState state)
        {
            register();
            cCOMM.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude);
            cNav_Master.updateStation(iStation_ID, cCOMM);
        }

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Sea Level Pressure (hPa)", guiFormat = "F2")]
        double dCurrent_Sea_Level_Pressure_hPa;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Local Temperature (C)", guiFormat = "F1")]
        double dCurrent_Temperature_C;


        private void Process_Timestep(double dTimestep)
        {
            string ResName = "ElectricCharge";
            double dElectric_Draw = ProcessorPowerRequired * dTimestep;
            double elecAvail = part.RequestResource(ResName, dElectric_Draw) / dElectric_Draw;
            dTime += dTimestep;
            double DayLen = 21600.000;
            double dDays = Math.Floor(Planetarium.GetUniversalTime() / DayLen);
            double dHourOfDay = (Planetarium.GetUniversalTime() - dDays * DayLen) / 3600.0;
            int HourOfDay = (int)Math.Truncate(dHourOfDay);
            if (elecAvail > 0.90) // at least 90% power
            {
                double dSea_Level_Pressure = cADM.computeSeaLevelPressure(vessel.staticPressurekPa * 1000.0, vessel.altitude); // assume that the altitude was correctly determined when it was installed
                dCurrent_Sea_Level_Pressure_hPa = dSea_Level_Pressure / 100.0;
                cADM.setSeaLevelPressure(dSea_Level_Pressure);
                cADM.performSample(vessel.staticPressurekPa * 1000.0, vessel.dynamicPressurekPa * 1000.0, vessel.externalTemperature, dTime);
                double dTemperature = cADM.getStaticAirTemp();
                dCurrent_Temperature_C = dTemperature - 273.15;
                if (HourOfDay != iLastMetarUpdateTime)
                {
                    dReported_Pressure_hPa = dCurrent_Sea_Level_Pressure_hPa;
                    dReported_Temperature_C = dCurrent_Temperature_C;

                    //@@TODO wind - for as of this version KSP has no wind
                    //@@TODO visibility - as of this version KSP always has 15 mi visibility
                    string sTemp = Math.Abs(dReported_Temperature_C).ToString("00");
                    string sDewpoint = Math.Abs(dReported_Temperature_C - 5.0).ToString("00"); //@@TODO: I think the KSP atmosphere is dry. I put this in here so there is a value other than -273.15 C
                    string sPress = "Q" + dReported_Pressure_hPa.ToString("0000");
                    string sT2;
                    string sD2;
                    if (dReported_Temperature_C < 0)
                        sT2 = "M" + sTemp;
                    else
                        sT2 = sTemp;
                    if ((dReported_Temperature_C - 5.0) < 0)
                        sD2 = "M" + sDewpoint;
                    else
                        sD2 = sDewpoint;
                    sReported_METAR = sStation_ID + " " + (dDays + 1.0).ToString("00") + HourOfDay.ToString("00") + "00Z" + " " + "00000MPS" + " " + "9999" + " " + "CLR" + " " + sT2 + "/" + sD2 + " " + sPress + " RMK AO1";
                    sReported_Weather_English = sStation_ID + " Automated Weather " + HourOfDay.ToString("00") + "00 Zulu wind calm visibility greater than ten thousand meters sky conditions clear temperature " + dReported_Temperature_C.ToString("#0") + " celcius dew point " + (dReported_Temperature_C - 5.0).ToString("#0") + " altimeter " + dReported_Pressure_hPa.ToString("0000");
                    iLastMetarUpdateTime = HourOfDay;
                }
            }
            double dBroadcast_Power = 0.0;
            if (bBroadcast_On)
            {
                dElectric_Draw = BroadcastPowerRequired * dTimestep;
                elecAvail = part.RequestResource(ResName, dElectric_Draw) / dElectric_Draw;
                if (elecAvail > 0.90)
                {
                    dBroadcast_Power = 10.0 * elecAvail;
                }
            }
            bool bUpdate_Flag = false;
            if (cCOMM.dTransmit_Power != dBroadcast_Power)
                bUpdate_Flag = true;
            cCOMM.dTransmit_Power = dBroadcast_Power;
            if (cCOMM.vPosition.updatePositionData(vessel.mainBody, vessel.latitude, vessel.longitude, vessel.altitude))
                bUpdate_Flag = true;


            if (dBroadcast_Power > 0)
            {
                sCurrent_Broadcast_METAR = sReported_METAR;
                sCurrent_Broadcast_Weather_English = sReported_Weather_English;
                if (cCOMM.sMessage != sCurrent_Broadcast_Weather_English)
                {
                    cCOMM.sMessage = sCurrent_Broadcast_Weather_English;
                    bUpdate_Flag = true;
                }
            }
            if (bUpdate_Flag)
                cNav_Master.updateStation(iStation_ID, cCOMM);

        }
        public override void OnUpdate()
        {
            Process_Timestep(Time.deltaTime);
        }
    }
}
