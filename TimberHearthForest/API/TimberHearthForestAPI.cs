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
