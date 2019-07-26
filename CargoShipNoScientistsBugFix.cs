using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CargoShipScientistsBugFix", "Ultra", "1.0.1")]
    [Description("Cargo ship respawns if it has spawned out of livable map")]

    class CargoShipNoScientistsBugFix : RustPlugin
    {
        private uint overBorderTolerance = 100;

        private void OnEntitySpawned(BaseEntity Entity)
        {
            if (Entity == null) return;

            if (Entity is CargoShip)
            {
                CargoShip cargoShip = (CargoShip)Entity;
                Vector3 newPosition = GetFixedPosition(cargoShip.transform.position);

                if (cargoShip.transform.position != newPosition)
                {
                    timer.Once(1f, () => { cargoShip.Kill(); });
                    timer.Once(2f, () => { SpawnCargoShip(newPosition); });
                }
            }
        }

        private Vector3 GetFixedPosition(Vector3 originalPosition)
        {
            int mapLimit = ConVar.Server.worldsize / 2;
            Vector3 newPosition = originalPosition;

            if (originalPosition.x < -(mapLimit) - overBorderTolerance) newPosition.x = -(mapLimit) - overBorderTolerance;
            if (originalPosition.x > mapLimit + overBorderTolerance) newPosition.x = mapLimit + overBorderTolerance;
            if (originalPosition.z < -(mapLimit) - overBorderTolerance) newPosition.z = -(mapLimit) - overBorderTolerance;
            if (originalPosition.z > mapLimit + overBorderTolerance) newPosition.z = mapLimit + overBorderTolerance;

            if (originalPosition != newPosition)
            {
                Puts(string.Format("CargoShip out of map position detected: {0} | {1} | {2}", originalPosition.x, originalPosition.y, originalPosition.z));
            }

            return newPosition;
        }

        private void SpawnCargoShip(Vector3 position)
        {
            Unsubscribe(nameof(OnEntitySpawned));
            var cargoShip = GameManager.server.CreateEntity("assets/content/vehicles/boats/cargoship/cargoshiptest.prefab") as CargoShip;
            if (cargoShip == null) return;
            cargoShip.transform.SetPositionAndRotation(position, new Quaternion());
            cargoShip.TriggeredEventSpawn();
            cargoShip.ServerPosition = position;
            cargoShip.Spawn();
            Subscribe(nameof(OnEntitySpawned));
            Puts(string.Format("New CargoShip spawned: {0} | {1} | {2}", cargoShip.transform.position.x, cargoShip.transform.position.y, cargoShip.transform.position.z));
        }
    }
}
