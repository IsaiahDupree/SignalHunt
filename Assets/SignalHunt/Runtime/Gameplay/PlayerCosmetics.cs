using UnityEngine;

namespace SignalHunt.Gameplay
{
    public static class PlayerCosmetics
    {
        private const string VehicleColorKey = "signalhunt.cosmetic.vehicle_color";

        public static int VehicleColorIndex
        {
            get => PlayerPrefs.GetInt(VehicleColorKey, 0);
            set
            {
                PlayerPrefs.SetInt(VehicleColorKey, Mathf.Abs(value) % 4);
                PlayerPrefs.Save();
            }
        }

        public static int CycleVehicleColor()
        {
            VehicleColorIndex++;
            return VehicleColorIndex;
        }
    }
}
