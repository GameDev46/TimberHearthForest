using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TimberHearthForest.API
{
    public interface ITimberHearthForestAPI
    {
        // MOD SETTINGS API FUNCTIONS
        public bool GetSettingsLocked();
        public void SetSettingsLocked(bool locked);

        public bool GetTreeCollidersEnabled();
        public void SetTreeCollidersEnabled(bool enabled);

        public void SetRegularCloudDensityPreset(string cloudDensityPreset);
        public void SetTreeDensityPreset(string treeDensityPreset);
        public void SetGrassDensityPreset(string grassDensityPreset);
        public void SetFireflyDensityPreset(string fireflyDensityPreset);

        public void GetFireflyVisibilityAtNight();
        public void SetFireflyVisibilityAtNight(bool enabled);
        public bool GetFireflyVisibilityAtDay();
        public void SetFireflyVisibilityAtDay(bool enabled);

        public float GetTotalTreeCount();
        public float GetActiveTreeCount();
        public float GetTotalGrassCount();
        public float GetActiveGrassCount();
        public float GetTotalFireflyCount();
        public float GetActiveFireflyCount();

        public bool GetTreeOptimisationEnabled();
        public void SetTreeOptimisationEnabled(bool enabled);

        public void SetLandingPadEnabled(bool enabled);

        public void SetBirdSongVolume(float volume);

        // GENERAL VOLUMETRIC CLOUD API FUNCTIONS
        public bool GetVolumetricCloudSettingsLocked();
        public void SetVolumetricCloudSettingsLocked(bool locked);

        public bool GetVolumetricCloudsEnabled();
        public void SetVolumetricCloudsEnabled(bool enabled);

        public float GetVolumetricCloudErosionStrength();
        public void SetVolumetricCloudErosionStrength(float strength);
        public float GetVolumetricCloudOuterRadius();
        public void SetVolumetricCloudOuterRadius(float radius);
        public float GetVolumetricCloudInnerRadius();
        public void SetVolumetricCloudInnerRadius(float radius);
        public float GetVolumetricCloudLightAbsorptionThroughCloud();
        public void SetVolumetricCloudLightAbsorptionThroughCloud(float absorption);
        public float GetVolumetricCloudLightAbsorptionTowardsSun();
        public void SetVolumetricCloudLightAbsorptionTowardsSun(float absorption);
        public bool GetVolumetricCloudShadowsEnabled();
        public void SetVolumetricCloudShadowsEnabled(bool enabled);

        // VOLUMETRIC CLOUD DYNAMIC WEATHER API FUNCTIONS
        public float GetVolumetricCloudScale();
        public void SetVolumetricCloudScale(float scale);
        public float GetVolumetricCloudCoverage();
        public void SetVolumetricCloudCoverage(float coverage);
        public float GetVolumetricCloudTurbulence();
        public void SetVolumetricCloudTurbulence(float turbulence);
        public bool GetDynamicWeatherEnabled();
        public void SetDynamicWeatherEnabled(bool enabled);
    }
}
