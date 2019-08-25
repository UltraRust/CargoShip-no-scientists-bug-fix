using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CargoShipScientistsBugFix", "Ultra", "2.1.3")]
    [Description("Cargo ship respawns if it has spawned out of livable map")]

    class CargoShipNoScientistsBugFix : RustPlugin
    {
        bool initialized = false;
        int mapLimit = 0;
        Timer currentPositionLogTimer = null;

        #region Hooks

        void OnServerInitialized()
        {
            mapLimit = (ConVar.Server.worldsize / 2) * 3;
            if (mapLimit > 4000) mapLimit = 4000;
            initialized = true;
            Log($"OnServerInitialized(): MapSize: {ConVar.Server.worldsize} / MapLimit set to {mapLimit}", logType: LogType.INFO);
        }

        void OnEntitySpawned(BaseEntity Entity)
        {
            if (!initialized) return;
            if (Entity == null) return;

            if (Entity is CargoShip)
            {
                CargoShip cargoShip = (CargoShip)Entity;

                if (!IsInLivableArea(cargoShip.transform.position))
                {
                    Log($"CargoShip spawned out liveable area", logType: LogType.WARNING);
                    Log($"{cargoShip.transform.position.x}|{cargoShip.transform.position.y}|{cargoShip.transform.position.z}", logType: LogType.WARNING);
                    timer.Once(1f, () => { cargoShip.Kill(); });
                    Vector3 newPostition = GetFixedPosition(cargoShip.transform.position);
                    timer.Once(2f, () => { SpawnCargoShip(newPostition); });
                }
                else
                {
                    Log($"CH47 spawned in liveable area properly", logType: LogType.INFO);
                    Log($"{cargoShip.transform.position.x}|{cargoShip.transform.position.y}|{cargoShip.transform.position.z}", logType: LogType.INFO);
                }
            }
        }

        void Unload()
        {
            if (currentPositionLogTimer != null)
            {
                currentPositionLogTimer.Destroy();
            }            
        }
        
        #endregion

        #region Core

        bool IsInLivableArea(Vector3 originalPosition)
        {
            if (originalPosition.x < -(mapLimit)) return false;
            if (originalPosition.x > mapLimit) return false;
            if (originalPosition.z < -(mapLimit)) return false;
            if (originalPosition.z > mapLimit) return false;
            return true;
        }

        Vector3 GetFixedPosition(Vector3 originalPosition)
        {
            Vector3 newPosition = originalPosition;
            if (originalPosition.x < -(mapLimit)) newPosition.x = -(mapLimit) + 100;
            if (originalPosition.x > mapLimit) newPosition.x = mapLimit - 100;
            if (originalPosition.z < -(mapLimit)) newPosition.z = -(mapLimit) + 100;
            if (originalPosition.z > mapLimit) newPosition.z = mapLimit - 100;
            return newPosition;
        }

        void SpawnCargoShip(Vector3 position)
        {
            Unsubscribe(nameof(OnEntitySpawned));
            var cargoShip = GameManager.server.CreateEntity("assets/content/vehicles/boats/cargoship/cargoshiptest.prefab") as CargoShip;
            if (cargoShip == null) return;
            cargoShip.transform.SetPositionAndRotation(position, new Quaternion());
            cargoShip.TriggeredEventSpawn();
            cargoShip.ServerPosition = position;
            cargoShip.Spawn();
            currentPositionLogTimer = timer.Repeat(30, 5, () => LogCurrentPosition(cargoShip));
            Subscribe(nameof(OnEntitySpawned));
            Log($"Standby CargoShip spawned: {cargoShip.transform.position.x}|{cargoShip.transform.position.y}|{cargoShip.transform.position.z}", logType: LogType.INFO);
        }

        void LogCurrentPosition(CargoShip cargoShip)
        {
            if (cargoShip == null || cargoShip.IsDestroyed) return;
            Log($"Current position: {cargoShip.transform.position.x}|{cargoShip.transform.position.y}|{cargoShip.transform.position.z} ", logType: LogType.INFO);
        }

        #endregion

        #region Config

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "LogInFile")]
            public bool LogInFile;

            [JsonProperty(PropertyName = "LogInConsole")]
            public bool LogInConsole;
        }

        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData()
            {
                LogInFile = true,
                LogInConsole = true
            };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(configData, true);
            base.SaveConfig();
        }

        #endregion

        #region Log

        void Log(string message, bool console = false, LogType logType = LogType.INFO, string fileName = "")
        {
            if (configData.LogInFile)
            {
                LogToFile(fileName, $"[{DateTime.Now.ToString("hh:mm:ss")}] {logType} > {message}", this);
            }

            if (configData.LogInConsole)
            {
                Puts($"{message.Replace("\n", " ")}");
            }
        }

        enum LogType
        {
            INFO = 0,
            WARNING = 1,
            ERROR = 2
        }

        #endregion
    }
}
