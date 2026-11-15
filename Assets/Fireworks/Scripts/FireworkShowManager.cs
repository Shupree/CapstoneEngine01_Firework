using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Capstone.Fireworks
{
    public enum LaunchPattern { Sequential, Simultaneous, Alternating }

    [Serializable]
    public class FireworkPalette
    {
        public string name = "Palette";
        public Gradient colors = new Gradient();
    }

    [Serializable]
    public class FireworkCue
    {
        public string name = "Cue";
        [Min(0)] public float delay = 1;
        public ParticleSystem shape;
        [Tooltip("-1 preserves the original prefab colors.")]
        public int paletteIndex = -1;
        public LaunchPattern pattern;
        [Range(1, 12)] public int count = 3;
        [Min(0.1f)] public float interval = 1.5f;
        [Min(0.1f)] public float emissionDuration = 4;
    }

    public sealed class FireworkShowManager : MonoBehaviour
    {
        [Header("Playback")]
        public bool playOnStart = true;
        public bool loop = true;
        [Min(0.1f)] public float loopPause = 4;
        [Range(1, 64)] public int maxActiveEffects = 18;

        [Header("Reusable shapes / drag Particle System prefabs here")]
        public ParticleSystem[] shapes;
        [Header("Reusable color palettes / -1 in a cue keeps original colors")]
        public FireworkPalette[] palettes;
        [Header("Launch positions / optional empty scene objects")]
        public Transform[] launchPoints;
        public float fallbackSpacing = 12;
        [Header("World scale / launch altitude and burst size")]
        [Range(0.25f, 4f)] public float effectScale = 1;
        [Header("Show timeline / delays are relative to the preceding cue")]
        public FireworkCue[] cues;

        private readonly List<ParticleSystem> active = new List<ParticleSystem>();
        private Coroutine show;
        public int ActiveEffectCount => active.Count;
        public int TotalLaunched { get; private set; }
        public bool IsPlaying => show != null;

        private void Start() { if (playOnStart) PlayShow(); }

        [ContextMenu("Play Show (Play Mode)")]
        public void PlayShow()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            StopShow();
            if (cues != null && cues.Length > 0) show = StartCoroutine(RunShow());
        }

        [ContextMenu("Stop And Clear (Play Mode)")]
        public void StopShow()
        {
            StopAllCoroutines();
            show = null;
            foreach (var ps in active) if (ps) Destroy(ps.gameObject);
            active.Clear();
        }

        private void OnDisable() { StopShow(); }

        private IEnumerator RunShow()
        {
            // Yield once so the coroutine handle is assigned before a short show completes.
            yield return null;
            do
            {
                foreach (var cue in cues)
                {
                    if (cue == null) continue;
                    yield return new WaitForSeconds(Mathf.Max(0, cue.delay));
                    yield return RunCue(cue);
                }
                if (loop) yield return new WaitForSeconds(Mathf.Max(0.1f, loopPause));
            } while (loop);
            show = null;
        }

        public void Launch(int shapeIndex, int paletteIndex, LaunchPattern pattern, int count = 3)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || shapes == null ||
                shapeIndex < 0 || shapeIndex >= shapes.Length) return;
            StartCoroutine(RunCue(new FireworkCue { shape = shapes[shapeIndex],
                paletteIndex = paletteIndex, pattern = pattern, count = count }));
        }

        private IEnumerator RunCue(FireworkCue cue)
        {
            if (!cue.shape) yield break;
            int count = Mathf.Clamp(cue.count, 1, 12);
            int pointCount = launchPoints != null && launchPoints.Length > 0 ? launchPoints.Length : 3;
            for (int i = 0; i < count; i++)
            {
                int point = cue.pattern == LaunchPattern.Alternating
                    ? (i % 2 == 0 ? (i / 2) % pointCount : pointCount - 1 - (i / 2) % pointCount)
                    : i % pointCount;
                Spawn(cue, point, pointCount);
                if (cue.pattern != LaunchPattern.Simultaneous && i + 1 < count)
                    yield return new WaitForSeconds(Mathf.Max(0.1f, cue.interval));
            }
        }

        private void Spawn(FireworkCue cue, int point, int pointCount)
        {
            active.RemoveAll(p => !p);
            if (active.Count >= Mathf.Max(1, maxActiveEffects)) return;
            Transform anchor = launchPoints != null && point < launchPoints.Length ? launchPoints[point] : null;
            Vector3 position = anchor ? anchor.position : transform.TransformPoint(
                new Vector3((point - (pointCount - 1) * 0.5f) * fallbackSpacing, 0, 0));
            Quaternion rotation = anchor ? anchor.rotation : transform.rotation;
            var root = Instantiate(cue.shape, position, rotation, transform);
            root.transform.localScale = cue.shape.transform.localScale * Mathf.Clamp(effectScale, 0.25f, 4f);
            root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            FireworkPalette palette = palettes != null && cue.paletteIndex >= 0 && cue.paletteIndex < palettes.Length
                ? palettes[cue.paletteIndex] : null;
            foreach (var ps in systems)
            {
                var lights = ps.lights;
                lights.enabled = false;
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (palette == null || palette.colors == null) continue;
                // Sub-emitters inherit and multiply colors in some reference shapes;
                // a white start color keeps the selected palette consistent.
                main.startColor = Color.white;
                var color = ps.colorOverLifetime;
                var original = color.color;
                var alphaKeys = color.enabled && original.mode == ParticleSystemGradientMode.Gradient
                    ? original.gradient.alphaKeys
                    : new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
                var recolored = new Gradient();
                recolored.SetKeys(palette.colors.colorKeys, alphaKeys);
                color.enabled = true;
                color.color = recolored;
                var speedColor = ps.colorBySpeed;
                speedColor.enabled = false;
                var trails = ps.trails;
                if (trails.enabled) trails.inheritParticleColor = true;
            }
            active.Add(root);
            TotalLaunched++;
            root.Play(true);
            StartCoroutine(Retire(root, Mathf.Max(0.1f, cue.emissionDuration)));
        }

        private IEnumerator Retire(ParticleSystem root, float duration)
        {
            yield return new WaitForSeconds(duration);
            // Stop only the launcher: existing shells must still trigger their death sub-emitters.
            if (root) root.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            float deadline = Time.time + 30;
            while (root && root.IsAlive(true) && Time.time < deadline) yield return null;
            active.Remove(root);
            if (root) Destroy(root.gameObject);
        }
    }
}
