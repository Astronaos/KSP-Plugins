using UnityEngine;
using System.IO;
using System;
using KSP_ADM;

namespace KSP_ADU
{
    public class AirDataUnit : PartModule
    {
        private AirDataModule cADM;

        private double dUpdate_Timer = 0.0;
        private double dTime = 0.0;
        private double dStatic_Press_Sea_Level = 101325.0;

        // Estimating from the Z-400 battery, which weights 20 kg (0.02 t) and has a charge of 400, and a "typical" Li battery that stores about 18 kJ in 20g, 
        // the battery contains 18 MJ, meaning 1 charge unit is about 45 kJ. This leads to charge demand 1 = 45 kW, or 0.00060 = 27 W, typical for a commercial aircraft ADU
        private double dW_to_EC = 1.0 / 45000.0;
        private double ElectricPowerRequired;

        private void initializeVariables()
        {
            if (cADM == null)
                cADM = new AirDataModule();
            cADM.initialize();
            cADM.setSeaLevelPressure(dStatic_Press_Sea_Level);
            ElectricPowerRequired = 25.0 * dW_to_EC;
        }

        public override void OnActive()
        {
            initializeVariables();
        }
        public override void OnAwake()
        {
            initializeVariables();
        }
        public override void OnInactive()
        {
            initializeVariables();
        }

        public override void OnStart(StartState state)
        {
            if ((state & StartState.PreLaunch) == StartState.PreLaunch)
            {
                initializeVariables();
                //dStatic_Press_Sea_Level = dStatic_Press_Sea_Level * Math.Exp(1.82312384e-04 * FlightGlobals.ship_altitude); // initialize static air pressure at sea level at at pre-launch
                dTime = 0.0;
            }
        }
        public override void OnUpdate()
        {
            string ResName = "ElectricCharge";
            double dElectric_Draw = ElectricPowerRequired * TimeWarp.deltaTime;
            double elecAvail = part.RequestResource(ResName, dElectric_Draw) / dElectric_Draw;
            dTime += TimeWarp.deltaTime;
            if (elecAvail > 0.90)
            {
                cADM.performSample(vessel.staticPressurekPa * 1000.0, vessel.dynamicPressurekPa * 1000.0, vessel.externalTemperature, dTime);


                dUpdate_Timer += TimeWarp.deltaTime;
                if (dUpdate_Timer > 1.0)
                {
                    dUpdate_Timer = 0.0;
                    using (StreamWriter sw = File.AppendText("adu.csv"))
                    {
                        string sPos = "ADU, " + dTime + ", " + FlightGlobals.ship_altitude + ", " + vessel.atmosphericTemperature + ", " + (vessel.staticPressurekPa * 1000.0) + ", " + (vessel.dynamicPressurekPa * 1000.0) + ", " + vessel.externalTemperature + ", " + cADM.getStaticAirTemp() + ", " + cADM.getSoundSpeed() + ", " + cADM.getAltitude() + ", " + cADM.getTrueAirspeed() + ", " + cADM.getCalibratedAirspeed() + ", " + cADM.getMachNumber();
                        sw.WriteLine(sPos);
                    }
                }
            }
        }
    }
}
