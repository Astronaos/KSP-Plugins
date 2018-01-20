using UnityEngine;
using System.IO;
using System;

namespace KSP_ADM
{
    public class AirDataModule
    {
        private double dNitrogen_Atomic_Weight = (14.00643 + 14.00728) * 0.5;
        private double dOxygen_Atomic_Weight = (15.99903 + 15.99977) * 0.5;
        private double dCarbon_Atomic_Weight = (12.0096 + 12.0116) * 0.5;
        private double dNeon_Atomic_Weight = 20.1797;
        private double dHelium_Atomic_Weight = 4.002602;
        private double dKrypton_Atomic_Weight = 83.798;
        private double dArgon_Atomic_Weight = 39.948;
        //       double dHydrogen_Atomic_Weight = (1.00784 + 1.00811) * 0.5;
        private double dMean_Mass_Air;
        private double dXenon_Atomic_Weight = 131.293;
        private double dStatic_Press_Sea_Level_Pa = -1;
        private const double Standard_Sea_Level_Temperature_K = 288.13; // or so...; note that this is probably location dependant - I measured it at KSC.
        private const double MicrowaveBackgroundTemperature_K = 4.0;
        private const double StandardGravitationalParameterKerbin = 3.5315984e+12;
        private const double BoltzmannConstant = 1.38064852e-23;
        private const double KerbinRadius = 600000.0;
        private double AtmosphericConstant;
        private double dGamma_Air = 1.4;

        private double[] adAltitudes;
        private double[] adAltitude_Times;

        private double[] dAltitude_Regime = { 0.0, 8815.0, 16050.0, 25700.0, 37880.0, 41130.0, 57000.0, 68800.0, 70000.0 };
        private double[]dLapse_Rates = {
                                    -8.11097078073758000e-3, // 0 -- 8815
                                     0.0,  // 8815 -- 16050
                                     1.23982456555866000e-3 , // 16050 -- 25700
                                     3.45670727834606000e-3 , // 25700 -- 37880
                                     0.0,  // 37880 -- 41130
                                     -3.43357895217294000e-3, // 41130 -- 57000
                                     -2.44340783421710000e-3,  // 57000 -- 68800
                                     0.0  // 68800 -- 70000
                                };
        private double[] dPressure_Table = { 0.0, 8815.0, 16050.0, 25700.0, 37880.0, 41130.0, 57000.0, 68800.0, 70000.0 }; // dummy values to set length
        private double[] dTemperature_Table = { 0.0, 8815.0, 16050.0, 25700.0, 37880.0, 41130.0, 57000.0, 68800.0, 70000.0 }; // dummy values to set length


        private double GetAltitude(double i_dMeasured_Pressure)
        {
            double dConst = AtmosphericConstant;
            double dInv_Const = 1.0 / dConst;
            double dAltitude = -50000.0;
            bool bDone = false;
            for (int i = 1; i < dPressure_Table.Length && !bDone; i++)
            {
                if (i_dMeasured_Pressure > dPressure_Table[i])
                {
                    if (dLapse_Rates[i - 1] != 0.0)
                    {
                        dAltitude = (Math.Pow(i_dMeasured_Pressure / dPressure_Table[i - 1],dLapse_Rates[i - 1] * dInv_Const) - 1.0) * dTemperature_Table[i - 1] / dLapse_Rates[i - 1] + dAltitude_Regime[i - 1];
                    }
                    else
                    {
                        dAltitude = dInv_Const * dTemperature_Table[i - 1] * Math.Log(i_dMeasured_Pressure / dPressure_Table[i - 1]) + dAltitude_Regime[i - 1];
                    }
                    bDone = true;
                }
            }
            return dAltitude;
        }
        public double KerbinStandardAtmosphereTemperature(double i_dSea_Level_Temperature_K, double i_dAltitude_m)
        {
            double dTemp = i_dSea_Level_Temperature_K;
            if (i_dAltitude_m > dAltitude_Regime[dAltitude_Regime.Length - 1])
                dTemp = MicrowaveBackgroundTemperature_K;
            else
            {
                for (int i = 1; i < dAltitude_Regime.Length && i_dAltitude_m > dAltitude_Regime[i - 1]; i++)
                {
                    double dAlt_Lcl = Math.Min(i_dAltitude_m, dAltitude_Regime[i]) - dAltitude_Regime[i - 1];
                    dTemp += dLapse_Rates[i - 1] * dAlt_Lcl;
                }
            }
            return dTemp;

        }
        public double KerbinStandardAtmospherePressure(double i_dSea_Level_Pressure_Pa, double i_dSea_Level_Temperature_K, double i_dAltitude_m)
        {
            double dConst = AtmosphericConstant;
            double dPress = i_dSea_Level_Pressure_Pa;
            if (i_dAltitude_m > dAltitude_Regime[dAltitude_Regime.Length - 1])
                dPress = 0.0; // above atmosphere
            else
            {
                for (int i = 1; i < dAltitude_Regime.Length && i_dAltitude_m > dAltitude_Regime[i - 1]; i++)
                {
                    double dLayer_Base_Temp = KerbinStandardAtmosphereTemperature(i_dSea_Level_Temperature_K, dAltitude_Regime[i - 1]);
                    double dAlt_Lcl = Math.Min(i_dAltitude_m, dAltitude_Regime[i]) - dAltitude_Regime[i - 1];
                    if (dLapse_Rates[i - 1] != 0.0)
                        dPress *= Math.Pow(1 + dLapse_Rates[i - 1] * dAlt_Lcl / dLayer_Base_Temp, dConst / dLapse_Rates[i - 1]);
                    else
                        dPress *= Math.Exp(dConst / dLayer_Base_Temp * dAlt_Lcl);
                    //string sData = "ADU " + i + " " + dAltitude_Regime[i - 1] + " " + dAltitude_Regime[i] + " " + i_dAltitude_m + " " + dAlt_Lcl + " " + dLayer_Base_Temp + " " + dLapse_Rates[i - 1] + " " + dPress + " " + dConst;
                    //print(sData);
                }
            }
            return dPress;

        }
        public double GetSoundSpeed(double i_dTemperature_K)
        {
            //return 20.046330927 * Math.Sqrt(i_dTemperature_K);
            return Math.Sqrt(2.0 * BoltzmannConstant * i_dTemperature_K / (dGamma_Air * dMean_Mass_Air));
        }


        private double dMach_Number;
        private double dStatic_Air_Temp;

        private double dAir_Number_Density;
        private double dAir_Density;

        private double dAltitude;

        private double dSpeed_Of_Sound;
        private double dTrue_Airspeed;
        private double dCalibrated_Airspeed;
        private double dVertical_Speed;

        public void setSeaLevelPressure(double i_dSea_Level_Pressure_Pa)
        {
            if (i_dSea_Level_Pressure_Pa != dStatic_Press_Sea_Level_Pa)
            {
                dStatic_Press_Sea_Level_Pa = i_dSea_Level_Pressure_Pa;
                for (int i = 0; i < dAltitude_Regime.Length; i++)
                {
                    dPressure_Table[i] = KerbinStandardAtmospherePressure(i_dSea_Level_Pressure_Pa, Standard_Sea_Level_Temperature_K, dAltitude_Regime[i]);
                    dTemperature_Table[i] = KerbinStandardAtmosphereTemperature(Standard_Sea_Level_Temperature_K, dAltitude_Regime[i]);
                }
            }
        }
        public void checkVSTables()
        {
            if (adAltitudes == null)
            {
                adAltitudes = new double[100];
                for (int i = 0; i < 100; i++)
                    adAltitudes[i] = 0;
            }
            if (adAltitude_Times == null)
            { 
                adAltitude_Times = new double[100];
                for (int i = 0; i < 100; i++)
                    adAltitude_Times[i] = -1;
            }
        }
        public void initialize()
        {
            dMean_Mass_Air = 1.660539040e-27 * (
                       2 * dNitrogen_Atomic_Weight * 0.7547 +
                       2 * dOxygen_Atomic_Weight * 0.2320 +
                       dArgon_Atomic_Weight * 0.0128 +
                       (dCarbon_Atomic_Weight + 2 * dOxygen_Atomic_Weight) * 0.00062 +
                       dNeon_Atomic_Weight * 0.000012 +
                       dHelium_Atomic_Weight * 0.0000007 +
                       dKrypton_Atomic_Weight * 0.000003 +
                       dXenon_Atomic_Weight * 0.0000004);
            dStatic_Press_Sea_Level_Pa = -1; // force table generation when setSeaLevelPressure called below
            AtmosphericConstant = -dMean_Mass_Air * StandardGravitationalParameterKerbin / (KerbinRadius * KerbinRadius) / BoltzmannConstant;
            checkVSTables();
            setSeaLevelPressure(101325.0);
        }

        public double computeSeaLevelPressure(double i_dStatic_Pressure_Pa, double i_dAltitude_m)
        {
            if (dStatic_Press_Sea_Level_Pa == -1)
                initialize();
            double dConst = AtmosphericConstant;


            double dPress = i_dStatic_Pressure_Pa;
            if (i_dAltitude_m > dAltitude_Regime[dAltitude_Regime.Length - 1])
                dPress = 101325.0; // above atmosphere - have no idea what is happening below
            else if (i_dAltitude_m < dAltitude_Regime[0])
            {
                double dLayer_Base_Temp = KerbinStandardAtmosphereTemperature(Standard_Sea_Level_Temperature_K, dAltitude_Regime[0]);
                dPress *= Math.Pow(1 + dLapse_Rates[0] * i_dAltitude_m / dLayer_Base_Temp, -dConst / dLapse_Rates[0]);
            }
            else
            {
                int i = dAltitude_Regime.Length - 1;
                for (; i >= 0 && i_dAltitude_m < dAltitude_Regime[i]; i--) ;
                double dAlt = i_dAltitude_m;
                for (; i >= 0; i--)
                {
                    double dAlt_Lcl = dAlt - dAltitude_Regime[i];
                    double dLayer_Base_Temp = KerbinStandardAtmosphereTemperature(Standard_Sea_Level_Temperature_K, dAltitude_Regime[i]);
                    if (dLapse_Rates[i] != 0.0)
                        dPress *= Math.Pow(1 + dLapse_Rates[i] * dAlt_Lcl / dLayer_Base_Temp, -dConst / dLapse_Rates[i]);
                    else
                        dPress *= Math.Exp(-dConst / dLayer_Base_Temp * dAlt_Lcl);
                    dAlt = dAltitude_Regime[i];
                }
            }
            return dPress;

        }

        public double getSeaLevelPressure() { return dStatic_Press_Sea_Level_Pa; }
        public double getMachNumber() { return dMach_Number; }
        public double getStaticAirTemp() { return dStatic_Air_Temp; }
        public double getAirDensity() { return dAir_Density; }
        public double getAltitude() { return dAltitude; }
        public double getSoundSpeed() { return dSpeed_Of_Sound; }
        public double getTrueAirspeed() { return dTrue_Airspeed; }
        public double getCalibratedAirspeed() { return dCalibrated_Airspeed; }
        public double getVerticalSpeed() { return dVertical_Speed; }
        public double getTotalAirTemp() { return dTotal_Air_Temp; }
        public double getTotalPressure() { return dTotal_Pressure; }
        public double getStaticPressure() { return dStatic_Pressure; }
        public double getDynamicPressure() { return dDynamic_Pressure; }
        public double getEquivalentAirspeed() { return dEquivalent_Airspeed; }
        private double dDynamic_Pressure;
        private double dTotal_Pressure;
        private double dStatic_Pressure;
        private double dTotal_Air_Temp;
        private double dEquivalent_Airspeed;

        public void performSample(double i_dStatic_Pressure_Pa, double i_dDynamic_Pressure_Pa, double i_dTotal_Air_Temperature_K, double i_dTime_s)
        {
            checkVSTables();
            if (dStatic_Press_Sea_Level_Pa == -1)
                initialize();
            dTotal_Pressure = i_dStatic_Pressure_Pa + i_dDynamic_Pressure_Pa;
            dStatic_Pressure = i_dStatic_Pressure_Pa;
            dDynamic_Pressure = i_dDynamic_Pressure_Pa;
            dTotal_Air_Temp = i_dTotal_Air_Temperature_K;
            if (i_dStatic_Pressure_Pa > 0.0) // quit working if out of atmosphere
            {
                double dGamma_Air = 1.4; // assume dry air
                dTotal_Air_Temp = i_dTotal_Air_Temperature_K;
                dMach_Number = Math.Sqrt(2.0 / (dGamma_Air - 1.0) * (Math.Pow(i_dDynamic_Pressure_Pa / i_dStatic_Pressure_Pa + 1.0, (dGamma_Air - 1.0) / dGamma_Air) - 1.0));
                if (dMach_Number > 1.0)
                {
                    double dPr = (i_dDynamic_Pressure_Pa / i_dStatic_Pressure_Pa + 1.0);
                    double dC = Math.Pow(dPr / 1.2, 0.4);
                    double dMach_Test = dMach_Number;
                    double dF;
                    int tCount = 0;
                    do
                    {
                        dF = 7.2 * Math.Pow(dMach_Test, 7.0) - (7.0 * dMach_Test * dMach_Test - 1.0) * dC;
                        double dFp = 7.2 * 7.0 * Math.Pow(dMach_Test, 6.0) - 7.0 * 2.0 * dMach_Test * dC;
                        dMach_Test -= dF / dFp;
                        tCount++;
                    } while (Math.Abs(dF) > 0.00001); // @@TODO figure out better threshold
                    dMach_Number = dMach_Test;
                }
                dCalibrated_Airspeed = GetSoundSpeed(288.15) * dMach_Number;

                dStatic_Air_Temp = i_dTotal_Air_Temperature_K / (1 + (dGamma_Air - 1.0) / 2.0 * dMach_Number * dMach_Number);
                // note: The SAT value here is as calculcated in a standard ADU; however it probably is not true at high mach numbers so it may be necessary to imrpove this
                double dSea_Level_Density = 101325.0 / (288.15 * BoltzmannConstant) * dMean_Mass_Air;
                dEquivalent_Airspeed = Math.Sqrt(2.0 * i_dDynamic_Pressure_Pa / dSea_Level_Density);


                dAltitude = GetAltitude(i_dStatic_Pressure_Pa);
                dSpeed_Of_Sound = GetSoundSpeed(dStatic_Air_Temp);
                dTrue_Airspeed = GetSoundSpeed(dTotal_Air_Temp) * (dMach_Number / Math.Sqrt(1.0 + (dGamma_Air - 1.0) / 2.0 * dMach_Number * dMach_Number));

                dAir_Number_Density = i_dStatic_Pressure_Pa / (dStatic_Air_Temp * BoltzmannConstant);
                dAir_Density = dAir_Number_Density * dMean_Mass_Air;

/*
                int i;
                for (i = 0; i < 100 && (i_dTime_s - adAltitude_Times[i]) < 2.0; i++) ;
                for (; i > 0; i--)
                {
                    adAltitudes[i] = adAltitudes[i - 1];
                    adAltitude_Times[i] = adAltitude_Times[i - 1];
                }
                adAltitudes[0] = dAltitude;
                adAltitude_Times[0] = i_dTime_s;
                
                dVertical_Speed = 0.0;
                i = 1;
                for (; i < 100; i++)
                {
                    if ((adAltitude_Times[i] - adAltitude_Times[0]) > 1.0)
                    {
                        dVertical_Speed = (adAltitudes[i] - adAltitudes[0]) / (adAltitude_Times[i] - adAltitude_Times[0]);
                        i = 100;
                    }
                }*/
            }
        }
    }
}
