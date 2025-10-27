using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FumoCore.Tools
{
    public static partial class ParticleSystemExtensions
    {
        public static void PlayIfNotPlaying(this ParticleSystem ps)
        {
            if (ps.isPlaying) return;
            ps.Play();
        }
        private static readonly Dictionary<ParticleSystem, ParticleSystem> particleCache = new();
        public static void EmitSingleCached(this ParticleSystem prefab, Vector3 position, Vector3? velocity = null, float lifetimeSpread = 0f, Color? colorOverride = null)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Particle System Extensions - " + nameof(EmitSingleCached) + " called with null prefab.");
                return;
            }

            if (!particleCache.TryGetValue(prefab, out var cached) || cached == null)
            {
                cached = GameObject.Instantiate(prefab);
                particleCache[prefab] = cached;
            }

            if (!cached.gameObject.activeInHierarchy)
                cached.gameObject.SetActive(true);

            var main = prefab.main;

            float baseLifetime = main.startLifetime.Evaluate();
            float finalLifetime = baseLifetime.Spread(lifetimeSpread);

            var emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity ?? Vector3.zero,
                startColor = colorOverride ?? main.startColor.Evaluate(),
                startSize = main.startSize.Evaluate(),
                startLifetime = finalLifetime,
            };

            cached.Emit(emitParams, 1);
        }
        private static float Evaluate(this ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode switch
            {
                ParticleSystemCurveMode.Constant => curve.constant,
                ParticleSystemCurveMode.TwoConstants => UnityEngine.Random.Range(curve.constantMin, curve.constantMax),
                ParticleSystemCurveMode.Curve => curve.curve.Evaluate(UnityEngine.Random.value),
                ParticleSystemCurveMode.TwoCurves =>
                    Mathf.Lerp(curve.curveMin.Evaluate(UnityEngine.Random.value),
                               curve.curveMax.Evaluate(UnityEngine.Random.value),
                               UnityEngine.Random.value),
                _ => 1f
            };
        }
        private static Color Evaluate(this ParticleSystem.MinMaxGradient gradient)
        {
            return gradient.mode switch
            {
                ParticleSystemGradientMode.Color => gradient.color,
                ParticleSystemGradientMode.TwoColors => UnityEngine.Color.Lerp(gradient.colorMin, gradient.colorMax, UnityEngine.Random.value),
                ParticleSystemGradientMode.Gradient => gradient.gradient.Evaluate(UnityEngine.Random.value),
                ParticleSystemGradientMode.TwoGradients =>
                    Color.Lerp(gradient.gradientMin.Evaluate(UnityEngine.Random.value),
                               gradient.gradientMax.Evaluate(UnityEngine.Random.value),
                               UnityEngine.Random.value),
                _ => Color.white
            };
        }
        [Initialize(-10000)]
        public static void InitializeParticleExtensions()
        {
            foreach (var ps in particleCache.Values)
            {
                if (ps != null)
                    Object.Destroy(ps.gameObject);
            }
            particleCache.Clear();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ParticleSystemExtensions.RevalidateCache();
        }
        private static void OnSceneUnloaded(Scene scene)
        {
            ParticleSystemExtensions.RevalidateCache();
        }
        public static void RevalidateCache()
        {
            var invalidParticleKeys = particleCache
                .Where(kvp => kvp.Key == null || kvp.Value == null)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in invalidParticleKeys)
                particleCache.Remove(key);

            foreach (var kvp in particleCache.ToList())
            {
                if (kvp.Value == null || kvp.Value.gameObject == null)
                {
                    particleCache.Remove(kvp.Key);
                }
            }

            var invalidArrayKeys = particleArrayCache
                .Where(kvp => kvp.Key == null)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in invalidArrayKeys)
                particleArrayCache.Remove(key);
        }
        private static readonly Dictionary<ParticleSystem, ParticleSystem.Particle[]> particleArrayCache = new();
        public static void RenderAnimatedPoints(this ParticleSystem ps, List<Vector2> positions, float animationDuration, bool staggerPhase = true)
        {
            if (ps == null || positions == null) return;

            int count = positions.Count;

            if (!particleArrayCache.TryGetValue(ps, out var particleArray) || particleArray.Length < count)
            {
                particleArray = new ParticleSystem.Particle[Mathf.Max(count, 64)];
                particleArrayCache[ps] = particleArray;
            }

            ParticleSystem.MainModule main = ps.main;
            Color startColor = main.startColor.color;
            float startSize = main.startSize.constant;

            float currentTime = Time.time;
            float baseRotationRad = main.startRotation.constant;
            float baseRotationDeg = -baseRotationRad * Mathf.Rad2Deg;

            for (int i = 0; i < count; i++)
            {
                if (particleArray[i].remainingLifetime <= 0f || particleArray[i].startLifetime != animationDuration)
                {
                    particleArray[i].startColor = startColor;
                    particleArray[i].startSize = startSize;
                    particleArray[i].startLifetime = animationDuration;
                    particleArray[i].rotation3D = new Vector3(0f, 0f, baseRotationDeg);
                    particleArray[i].velocity = Vector3.zero;
                }

                particleArray[i].position = new Vector3(positions[i].x, positions[i].y, 0f);

                float phaseOffset = 0f;
                if (staggerPhase && count > 1)
                    phaseOffset = (animationDuration / count) * i;

                float timeInCycle = (currentTime + phaseOffset) % animationDuration;
                float remainingLifetime = animationDuration - timeInCycle;

                remainingLifetime = Mathf.Max(0.001f, remainingLifetime);

                particleArray[i].remainingLifetime = remainingLifetime;
            }

            ps.SetParticles(particleArray, count, 0);
            if (!ps.isPlaying)
                ps.Play();
        }
    }
}
