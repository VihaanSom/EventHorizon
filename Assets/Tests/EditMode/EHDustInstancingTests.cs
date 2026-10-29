using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using EventHorizon.Environment;

namespace EventHorizon.Tests
{
    /// <summary>
    /// Regression test for the Play Mode crash:
    /// "InvalidOperationException: Material needs to enable instancing for
    /// use with DrawMeshInstanced" thrown from EHDustController.Update.
    /// Fails if either the code guarantee or the shipped material flag regresses.
    /// Run in the Test Runner (Window -> General -> Test Runner, EditMode tab).
    /// </summary>
    public class EHDustInstancingTests
    {
        const BindingFlags AnyInstance =
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        [Test]
        public void Awake_EnablesInstancing_OnBothMaterials()
        {
            var go = new GameObject("EH_TestDust");
            try
            {
                var ctrl = go.AddComponent<EHDustController>();
                var dust = new Material(Shader.Find("EventHorizon/Dust"));
                var streak = new Material(Shader.Find("EventHorizon/Dust"));
                dust.enableInstancing = false;
                streak.enableInstancing = false;

                typeof(EHDustController).GetField("dustMaterial", AnyInstance)
                    .SetValue(ctrl, dust);
                typeof(EHDustController).GetField("streakMaterial", AnyInstance)
                    .SetValue(ctrl, streak);
                typeof(EHDustController).GetMethod("Awake", AnyInstance)
                    .Invoke(ctrl, null);

                Assert.IsTrue(dust.enableInstancing, "dust material must opt into instancing");
                Assert.IsTrue(streak.enableInstancing, "streak material must opt into instancing");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ShippedMaterials_HaveInstancingEnabled()
        {
            var dust = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/EventHorizon/Materials/EH_Dust.mat");
            var streak = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/EventHorizon/Materials/EH_Streak.mat");
            Assert.IsNotNull(dust, "EH_Dust.mat must exist");
            Assert.IsNotNull(streak, "EH_Streak.mat must exist");
            Assert.IsTrue(dust.enableInstancing, "EH_Dust.mat flag regressed");
            Assert.IsTrue(streak.enableInstancing, "EH_Streak.mat flag regressed");
        }
    }
}
