﻿using AdvancedTimers;
using EventArgsLibrary;
using HeatMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;
using WorldMap;

namespace WayPointGenerator
{
    public class WaypointGenerator
    {
        string robotName;

        //Timer timerWayPointGeneration;

        Location destinationLocation;
        GlobalWorldMap globalWorldMap;
        //double[,] strategyManagerHeatMap = new double[0, 0];

        //double heatMapCellsize = 2; //doit être la même que celle du strategy manager
        //double fieldLength = 22;
        //double fieldHeight = 14;

        Heatmap StrategyHeatmap; 

        public WaypointGenerator(string name)
        {
            robotName = name;
            //timerWayPointGeneration = new Timer(100);
            //timerWayPointGeneration.Elapsed += TimerWayPointGeneration_Elapsed;
            //timerWayPointGeneration.Start();

            waypointHeatMap = new Heatmap(22.0, 14.0, 22.0 / Math.Pow(2, 8), 2);
        }

        public void SetNextWayPoint(Location waypointLocation)
        {
            OnWaypoint(robotName, waypointLocation);
        }

        public void OnDestinationReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (e.RobotName == robotName)
            {
                destinationLocation = e.Location;
            }
        }

        public void OnStrategyHeatMapReceived(object sender, EventArgsLibrary.HeatMapArgs e)
        {            
            if (robotName == e.RobotName)
            {
                StrategyHeatmap = e.HeatMap;
                CalculateOptimalWayPoint();
            }
        }
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMap = e.GlobalWorldMap;
        }

        Stopwatch sw = new Stopwatch();
        Heatmap waypointHeatMap;
        private void CalculateOptimalWayPoint()
        {
            //Heatmap StrategyHeatmap = StrategyHeatmap.Copy();

            //Génération de la HeatMap
            //Heatmap heatMap = new Heatmap(22, 14, 0.01);            
            sw.Start(); // début de la mesure

            //Génération de la HeatMap
            waypointHeatMap.ReInitHeatMapData();
            int[] nbComputationsList = new int[waypointHeatMap.nbIterations];

            //On construit le heatMap en mode multi-résolution :
            //On commence par une heatmap très peu précise, puis on construit une heat map de taille réduite plus précise autour du point chaud,
            //Puis on construit une heatmap très précise au cm autour du point chaud.
            double optimizedAreaSize;

            PointD OptimalPosition = new PointD(0, 0);
            PointD OptimalPosInBaseHeatMapCoordinates = waypointHeatMap.GetBaseHeatMapPosFromFieldCoordinates(0, 0);

            for (int n = 0; n < waypointHeatMap.nbIterations; n++)
            {
                double subSamplingRate = waypointHeatMap.SubSamplingRateList[n];
                if (n >= 1)
                    optimizedAreaSize = waypointHeatMap.nbCellInSubSampledHeatMapWidthList[n] / waypointHeatMap.nbCellInSubSampledHeatMapWidthList[n - 1];
                else
                    optimizedAreaSize = waypointHeatMap.nbCellInSubSampledHeatMapWidthList[n];

                optimizedAreaSize /= 2;

                double minY = Math.Max(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate - optimizedAreaSize, 0);
                double maxY = Math.Min(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate + optimizedAreaSize, Math.Min(waypointHeatMap.nbCellInSubSampledHeatMapHeightList[n], waypointHeatMap.nbCellInBaseHeatMapHeight));
                double minX = Math.Max(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate - optimizedAreaSize, 0);
                double maxX = Math.Min(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate + optimizedAreaSize, Math.Min(waypointHeatMap.nbCellInSubSampledHeatMapWidthList[n], waypointHeatMap.nbCellInBaseHeatMapWidth));

                double max = double.NegativeInfinity;
                int maxXpos = 0;
                int maxYpos = 0;

                for (double y = minY; y < maxY; y += 1)
                {
                    for (double x = minX; x < maxX; x += 1)
                    {
                        //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
                        //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y)) / 20.0);
                        var heatMapPos = waypointHeatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y, n);
                        double pen = CalculPenalisation(heatMapPos); 
                        //double value = EvaluateStrategyCostFunction(robotRole, heatMapPos);
                        //heatMap.SubSampledHeatMapData1[y, x] = value;
                        int yBase = (int)(y * subSamplingRate);
                        int xBase = (int)(x * subSamplingRate);
                        double value = StrategyHeatmap.BaseHeatMapData[yBase, xBase] - pen;
                        waypointHeatMap.BaseHeatMapData[yBase, xBase] = value;
                        nbComputationsList[n]++;

                        if (value > max)
                        {
                            max = value;
                            maxXpos = xBase;
                            maxYpos = yBase;
                        }

                        //Code ci-dessous utile si on veut afficher la heatmap complete(video), mais consommateur en temps
                        for (int i = 0; i < waypointHeatMap.SubSamplingRateList[n]; i += 1)
                        {
                            for (int j = 0; j < waypointHeatMap.SubSamplingRateList[n]; j += 1)
                            {
                                if ((xBase + j < waypointHeatMap.nbCellInBaseHeatMapWidth) && (yBase + i < waypointHeatMap.nbCellInBaseHeatMapHeight))
                                    waypointHeatMap.BaseHeatMapData[yBase + i, xBase + j] = value;
                            }
                        }
                    }
                }
                //OptimalPosInBaseHeatMapCoordinates = heatMap.GetMaxPositionInBaseHeatMapCoordinates();
                OptimalPosInBaseHeatMapCoordinates = new PointD(maxXpos, maxYpos);
            }

            //var OptimalPosition = heatMap.GetMaxPositionInBaseHeatMap();
            OptimalPosition = waypointHeatMap.GetFieldPosFromBaseHeatMapCoordinates(OptimalPosInBaseHeatMapCoordinates.X, OptimalPosInBaseHeatMapCoordinates.Y);

            //var OptimalPosition = destinationLocation;

            OnHeatMap(robotName, waypointHeatMap);            
            if(OptimalPosition != null)
                SetNextWayPoint(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));

            sw.Stop(); // Fin de la mesure
            for (int n = 0; n < nbComputationsList.Length; n++)
            {
                Console.WriteLine("Calcul WayPoint - Nb Calculs Etape " + n + " : " + nbComputationsList[n]);
            }
            Console.WriteLine("Temps de calcul de la heatMap WayPoint : " + (sw.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond).ToString("N4")); // Affichage de la mesure
        }

        double CalculPenalisation(PointD ptCourant)
        {
            double penalisation = 0;
            if (globalWorldMap != null)
            {
                if (globalWorldMap.robotLocationDictionary.ContainsKey(robotName))
                {
                    Location robotLocation = globalWorldMap.robotLocationDictionary[robotName];
                    double angleDestination = Math.Atan2(destinationLocation.Y - robotLocation.Y, destinationLocation.X - robotLocation.X);

                    //On veut éviter de taper les autres robots
                    //for (int i = 0; i < globalWorldMap.robotLocationDictionary.Count; i++)
                    //{
                    //    string competitorName = globalWorldMap.robotLocationDictionary.Keys.ElementAt(i);
                    //    Location competitorLocation = globalWorldMap.robotLocationDictionary.Values.ElementAt(i);
                    lock (globalWorldMap.robotLocationDictionary)
                    {
                        foreach (var robot in globalWorldMap.robotLocationDictionary)
                        {
                            string competitorName = robot.Key;
                            Location competitorLocation = robot.Value;

                            //On itère sur tous les robots sauf celui-ci
                            if (competitorName != robotName)
                            {
                                double angleRobotAdverse = Math.Atan2(competitorLocation.Y - robotLocation.Y, competitorLocation.X - robotLocation.X);
                                double distanceRobotAdverse = Toolbox.Distance(competitorLocation.X, competitorLocation.Y, robotLocation.X, robotLocation.Y);


                                //PointD ptCourant = GetFieldPosFromHeatMapCoordinates(x, y);
                                double distancePt = Toolbox.Distance(ptCourant.X, ptCourant.Y, robotLocation.X, robotLocation.Y);
                                double anglePtCourant = Math.Atan2(ptCourant.Y - robotLocation.Y, ptCourant.X - robotLocation.X);

                                if (Math.Abs(distanceRobotAdverse * (anglePtCourant - angleRobotAdverse)) < 2.0 && distancePt > distanceRobotAdverse - 3)
                                    penalisation += 1;// Math.Max(0, 1 - Math.Abs(anglePtCourant - angleRobotAdverse) *10.0);

                            }
                        }
                    }
                }
            }
            return penalisation;
        }


        //private PointD GetFieldPosFromHeatMapCoordinates(int x, int y)
        //{
        //    return new PointD(-fieldLength / 2 + x * heatMapCellsize, -fieldHeight / 2 + y * heatMapCellsize);
        //}

        public delegate void NewWayPointEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnWaypointEvent;
        public virtual void OnWaypoint(string name, Location wayPointlocation)
        {
            var handler = OnWaypointEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotName = name, Location = wayPointlocation});
            }
        }


        public delegate void HeatMapEventHandler(object sender, HeatMapArgs e);
        public event EventHandler<HeatMapArgs> OnHeatMapEvent;
        public virtual void OnHeatMap(string name, Heatmap heatMap)
        {
            var handler = OnHeatMapEvent;
            if (handler != null)
            {
                handler(this, new HeatMapArgs { RobotName = name, HeatMap = heatMap });
            }
        }
    }
}
