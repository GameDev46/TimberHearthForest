using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TimberHearthForest.API
{
    public class TimberHearthForestAPI : ITimberHearthForestAPI
    {
        private readonly TimberHearthForest _mod;

        public TimberHearthForestAPI(TimberHearthForest mod)
        {
            _mod = mod;
        }

        // MOD SETTINGS API FUNCTIONS

        // Settings Lock
        public bool GetSettingsLocked() => _mod.GetSettingsLocked();
        public void SetSettingsLocked(bool locked) => _mod.SetSettingsLocked(locked);

        public bool GetTreeCollidersEnabled() => _mod.GetTreeCollidersEnabled();
        public void SetTreeCollidersEnabled(bool enabled) => _mod.SetTreeCollidersEnabled(enabled);

        public void SetRegularCloudDensityPreset(string cloudDensityPreset) => _mod.SetRegularCloudDensityPreset(cloudDensityPreset);
        public void SetTreeDensityPreset(string treeDensityPreset) => _mod.SetTreeDensityPreset(treeDensityPreset);
        public void SetGrassDensityPreset(string grassDensityPreset) => _mod.SetGrassDensityPreset(grassDensityPreset);
        public void SetFireflyDensityPreset(string fireflyDensityPreset) => _mod.SetFireflyDensityPreset(fireflyDensityPreset);

        public void GetFireflyVisibilityAtNight() => _mod.GetFireflyVisibilityAtNight();
        public void SetFireflyVisibilityAtNight(bool enabled) => _mod.SetFireflyVisibilityAtNight(enabled);
        public bool GetFireflyVisibilityAtDay() => _mod.GetFireflyVisibilityAtDay();
        public void SetFireflyVisibilityAtDay(bool enabled) => _mod.SetFireflyVisibilityAtDay(enabled);

        public float GetTotalTreeCount() => _mod.GetTotalTreeCount();
        public float GetActiveTreeCount() => _mod.GetActiveTreeCount();
        public float GetTotalGrassCount() => _mod.GetTotalGrassCount();
        public float GetActiveGrassCount() => _mod.GetActiveGrassCount();
        public float GetTotalFireflyCount() => _mod.GetTotalFireflyCount();
        public float GetActiveFireflyCount() => _mod.GetActiveFireflyCount();

        public bool GetTreeOptimisationEnabled() => _mod.GetTreeOptimisationEnabled();
        public void SetTreeOptimisationEnabled(bool enabled) => _mod.SetTreeOptimisationEnabled(enabled);

        public void SetLandingPadEnabled(bool enabled) => _mod.SetLandingPadEnabled(enabled);

        public void SetBirdSongVolume(float volume) => _mod.SetBirdSongVolume(volume);

        // GENERAL VOLUMETRIC CLOUD API FUNCTIONS

        // Volumetric Cloud Settings Lock
        public bool GetVolumetricCloudSettingsLocked() => _mod.GetVolumetricCloudSettingsLocked();
        public void SetVolumetricCloudSettingsLocked(bool locked) => _mod.SetVolumetricCloudSettingsLocked(locked);

        // Volumetric Clouds Enabled
        public bool GetVolumetricCloudsEnabled() => _mod.GetVolumetricCloudsEnabled();
        public void SetVolumetricCloudsEnabled(bool enabled) => _mod.SetVolumetricCloudsEnabled(enabled);

        // Volumetric Cloud Erosion Strength
        public float GetVolumetricCloudErosionStrength() => _mod.GetVolumetricCloudErosionStrength();
        public void SetVolumetricCloudErosionStrength(float strength) => _mod.SetVolumetricCloudErosionStrength(strength);

        // Volumetric Cloud Outer Radius
        public float GetVolumetricCloudOuterRadius() => _mod.GetVolumetricCloudOuterRadius();
        public void SetVolumetricCloudOuterRadius(float radius) => _mod.SetVolumetricCloudOuterRadius(radius);

        // Volumetric Cloud Inner Radius
        public float GetVolumetricCloudInnerRadius() => _mod.GetVolumetricCloudInnerRadius();
        public void SetVolumetricCloudInnerRadius(float radius) => _mod.SetVolumetricCloudInnerRadius(radius);

        // Volumetric Cloud Light Absorption Through Cloud
        public float GetVolumetricCloudLightAbsorptionThroughCloud() => _mod.GetVolumetricCloudLightAbsorptionThroughCloud();
        public void SetVolumetricCloudLightAbsorptionThroughCloud(float absorption) => _mod.SetVolumetricCloudLightAbsorptionThroughCloud(absorption);

        // Volumetric Cloud Light Absorption Towards Sun
        public float GetVolumetricCloudLightAbsorptionTowardsSun() => _mod.GetVolumetricCloudLightAbsorptionTowardsSun();
        public void SetVolumetricCloudLightAbsorptionTowardsSun(float absorption) => _mod.SetVolumetricCloudLightAbsorptionTowardsSun(absorption);

        // Volumetric Cloud Shadows
        public bool GetVolumetricCloudShadowsEnabled() => _mod.GetVolumetricCloudShadowsEnabled();
        public void SetVolumetricCloudShadowsEnabled(bool enabled) => _mod.SetVolumetricCloudShadowsEnabled(enabled);

        // Volumetric Cloud Scale
        public float GetVolumetricCloudScale() => _mod.GetVolumetricCloudScale();
        public void SetVolumetricCloudScale(float scale) => _mod.SetVolumetricCloudScale(scale);

        // Volumetric Cloud Coverage
        public float GetVolumetricCloudCoverage() => _mod.GetVolumetricCloudCoverage();
        public void SetVolumetricCloudCoverage(float coverage) => _mod.SetVolumetricCloudCoverage(coverage);

        // Volumetric Cloud Turbulence
        public float GetVolumetricCloudTurbulence() => _mod.GetVolumetricCloudTurbulence();
        public void SetVolumetricCloudTurbulence(float turbulence) => _mod.SetVolumetricCloudTurbulence(turbulence);

        // Volumetric Cloud Dyamic Weather
        public bool GetDynamicWeatherEnabled() => _mod.GetDynamicWeatherEnabled();
        public void SetDynamicWeatherEnabled(bool enabled) => _mod.SetDynamicWeatherEnabled(enabled);
    }
}
