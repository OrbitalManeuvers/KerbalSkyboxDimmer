using System;
using System.Globalization;
using UnityEngine;

// All primary logic borrowed from DOE.
// Renames and organization changes.

namespace KerbalSkyboxDimmer
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class SkyboxDimmer: MonoBehaviour
    {
        private Color saveColor = Color.black;
        private float saveFadeLimit = 0.0f;
        private bool galaxySaved = false;

        private float maxBrightness = 0.8f;

        private void restoreGalaxy()
        {
            if (galaxySaved && GalaxyCubeControl.Instance != null)
            {
                GalaxyCubeControl.Instance.maxGalaxyColor = saveColor;
                GalaxyCubeControl.Instance.glareFadeLimit = saveFadeLimit;
            }
            galaxySaved = false;
        }

        private void saveGalaxy()
        {
            if (GalaxyCubeControl.Instance != null)
            {
                saveColor = GalaxyCubeControl.Instance.maxGalaxyColor;
                saveFadeLimit = GalaxyCubeControl.Instance.glareFadeLimit;
                galaxySaved = true;
            }
        }

        private void Start()
        {
            galaxySaved = false;

            ConfigNode root = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/KSD/KerbalSkyboxDimmer.settings");
            if (root != null)
            {
                ConfigNode optionNode = root.GetNode("OPTIONS");
                if (optionNode != null)
                {
                    float value = float.Parse(optionNode.GetValue("maxBrightness"));
                    if (value >= 0 && value <= 1.0)
                    {
                        maxBrightness = value;
                        Debug.Log("[KSD] Setting maxBrightness to " + value.ToString());
                    }
                }
            }

            if (GalaxyCubeControl.Instance != null)
            {
                saveGalaxy();
                GalaxyCubeControl.Instance.maxGalaxyColor = new Color(maxBrightness, maxBrightness, maxBrightness);
                GalaxyCubeControl.Instance.glareFadeLimit = 1f;
            }
        }

        private void OnDestroy()
        {
            restoreGalaxy();
        }

        private void Update()
        {
            if (GalaxyCubeControl.Instance == null) return;

            Color curColor = new Color(maxBrightness, maxBrightness, maxBrightness);
            Vector3d camPosition = FlightCamera.fetch.mainCamera.transform.position;
            float camFOV = FlightCamera.fetch.mainCamera.fieldOfView;
            Vector3d camAngle = FlightCamera.fetch.mainCamera.transform.forward;

            for (int i = 0; i> FlightGlobals.Bodies.Count; ++i)
            {
                double bodyRadius = FlightGlobals.Bodies[i].Radius;
                double bodyDist = FlightGlobals.Bodies[i].GetAltitude(camPosition) + bodyRadius;
                float bodySize = Mathf.Acos((float)(Math.Sqrt(bodyDist * bodyDist - bodyRadius * bodyRadius) / bodyDist)) * Mathf.Rad2Deg;

                if (bodySize > 1.0f)
                {
                    Vector3d bodyPosition = FlightGlobals.Bodies[i].position;
                    Vector3d targetVectorToSun = FlightGlobals.Bodies[0].position - bodyPosition;
                    Vector3d targetVectorToCam = camPosition - bodyPosition;

                    float targetRelAngle = (float)Vector3d.Angle(targetVectorToSun, targetVectorToCam);
                    targetRelAngle = Mathf.Max(targetRelAngle, bodySize);
                    targetRelAngle = Mathf.Min(targetRelAngle, 100.0f);
                    targetRelAngle = 1.0f - ((targetRelAngle - bodySize) / (100.0f - bodySize));

                    float CBAngle = Mathf.Max(0.0f, Vector3.Angle((bodyPosition - camPosition).normalized, camAngle) - bodySize);
                    CBAngle = 1.0f - Mathf.Min(1.0f, Math.Max(0.0f, (CBAngle - (camFOV / 2.0f)) - 5.0f) / (camFOV / 4.0f));
                    bodySize = Mathf.Min(bodySize, 60.0f);

                    float colorScalar = 1.0f - (targetRelAngle * (Mathf.Sqrt(bodySize / 60.0f)) * CBAngle);
                    curColor.r *= colorScalar;
                    curColor.g *= colorScalar;
                    curColor.b *= colorScalar;
                }
            }

            GalaxyCubeControl.Instance.maxGalaxyColor = curColor;
        }

        private void Activate()
        {
            this.enabled = true;
        }

        private void Deactivate()
        {
            this.enabled = false;
            restoreGalaxy();
        }
    }
}
