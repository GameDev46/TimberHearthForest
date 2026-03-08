using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TimberHearthForest
{
    internal class ForestSectorUtils
    {
        private const int SECTOR_SIZE = 100;

        public struct ForestSector
        {
            public GameObject sectorParent;
            public Vector3Int sectorCoordinates;
        }

        public static ForestSector CreateSector(GameObject sectorsParent, Vector3Int sectorCoords)
        {
            // Used to group tree clones together for a cleaner hierachy
            GameObject sectorParent = new GameObject($"TH_Forest_Sector_{sectorCoords.x}_{sectorCoords.y}_{sectorCoords.z}");
            sectorParent.transform.SetParent(sectorsParent.transform, false);
            sectorParent.transform.localPosition = Vector3.zero;
            sectorParent.transform.localRotation = Quaternion.identity;

            // Used to group tree clones together for a cleaner hierachy
            GameObject treeParent = new GameObject("TH_Trees_Surface");
            treeParent.transform.SetParent(sectorParent.transform, false);
            treeParent.transform.localPosition = Vector3.zero;
            treeParent.transform.localRotation = Quaternion.identity;

            // Used to group grass clones together for a cleaner hierachy
            GameObject grassParent = new GameObject("TH_Grass_Surface");
            grassParent.transform.SetParent(sectorParent.transform, false);
            grassParent.transform.localPosition = Vector3.zero;
            grassParent.transform.localRotation = Quaternion.identity;

            // Used to group firefly clones together for a cleaner hierachy
            GameObject firefliesParent = new GameObject("TH_Fireflies_Surface");
            firefliesParent.transform.SetParent(sectorParent.transform, false);
            firefliesParent.transform.localPosition = Vector3.zero;
            firefliesParent.transform.localRotation = Quaternion.identity;

            ForestSector forestSector = new ForestSector();
            forestSector.sectorParent = sectorParent;
            forestSector.sectorCoordinates = sectorCoords;

            return forestSector;
        }

        public static bool IsSectorVisible(ForestSector sector, Vector3Int[] cameraSectorCoords, float playerTHDistance)
        {
            GameObject sectorHolder = sector.sectorParent;
            Vector3Int sectorCoords = sector.sectorCoordinates;

            const float MAX_DISTANCE = 800.0f;
            const float MIN_DISTANCE = 250.0f;

            // When the player is closer to Timber Hearth, more trees are hidden by the horizon
            const float FAR_DOT = -0.4f;
            const float CLOSE_DOT = 0.3f;

            float playerDistanceFract = Mathf.Clamp01((playerTHDistance - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE));
            float currentDot = Mathf.Lerp(CLOSE_DOT, FAR_DOT, playerDistanceFract);

            float maxDot = -10.0f;

            foreach (Vector3Int coord in cameraSectorCoords)
            {
                // Props which are blocked from the player's view by Timber Hearth are hidden
                float dot = (float)Dot(sectorCoords, coord);
                maxDot = Mathf.Max(maxDot, dot);
            }

            // Occlusion only occurs when neither the probe, satellite or player can see the tree group
            bool isOccluded = maxDot < currentDot;

            return !isOccluded;
        }

        public static Vector3Int GetSectorCoordsFromTHCoords(Vector3 THCoords)
        {
            int x = Mathf.RoundToInt(THCoords.x / (float)SECTOR_SIZE);
            int y = Mathf.RoundToInt(THCoords.y / (float)SECTOR_SIZE);
            int z = Mathf.RoundToInt(THCoords.z / (float)SECTOR_SIZE);

            Vector3Int coords = new Vector3Int(x, y, z);
            return coords;
        }

        public static Vector3 GetTHCoordsFromSector(Vector3Int SectorCoords)
        {
            float x = (float)(SectorCoords.x * SECTOR_SIZE);
            float y = (float)(SectorCoords.y * SECTOR_SIZE);
            float z = (float)(SectorCoords.z * SECTOR_SIZE);

            Vector3 coords = new Vector3(x, y, z);
            return coords;
        }

        public static int GetSectorSize()
        {
            return SECTOR_SIZE;
        }

        private static float Dot(Vector3 a, Vector3 b)
        {
            // Compute the dot product between vector a and b
            return (a.x * b.x + a.y * b.y + a.z * b.z);
        }

        private static int Dot(Vector3Int a, Vector3Int b)
        {
            // Compute the dot product between vector a and b
            return (a.x * b.x + a.y * b.y + a.z * b.z);
        }
    }
}
