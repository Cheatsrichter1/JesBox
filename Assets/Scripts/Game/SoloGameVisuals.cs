using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JesBox.Game
{
    /// <summary>
    /// Implemented by whatever renders a Chosen One turn's unique look — a 2D
    /// sprite scene, a 3D diorama rendered to a texture, whatever the game
    /// calls for. GameManager owns all scoring/win logic and only ever talks
    /// to this interface, so it doesn't need to know or care which style of
    /// visual is behind it.
    /// </summary>
    public interface ISoloGameVisual
    {
        /// <summary>Called once when the turn starts. <paramref name="stage"/>
        /// is the existing on-screen stage area every Chosen One game shares —
        /// implementations should place their on-screen display (a RawImage,
        /// a sprite, whatever) as a child of it so it lines up with everyone
        /// else's turn.</summary>
        void Setup(RectTransform stage);

        /// <summary>Called whenever the underlying game's progress changes,
        /// 0 (just started) to 1 (about to win). Purely cosmetic — this never
        /// decides win/lose, GameManager does that.</summary>
        void SetProgress(float fraction);

        /// <summary>Called once when the turn ends, win or lose. Release
        /// everything this visual created here, including anything living
        /// outside the stage hierarchy (a dedicated camera, a RenderTexture).</summary>
        void Teardown();
    }

    /// <summary>
    /// Optional extension for a visual that also has something to react when
    /// the chosen player successfully swings their phone — e.g. Parting the
    /// Sea's cone, tilted left/right like a Wii remote. GameManager checks
    /// for this via an `is` pattern rather than putting it on every visual,
    /// since most Chosen One games don't have anything like this.
    /// </summary>
    public interface ISteerableSoloGameVisual : ISoloGameVisual
    {
        /// <summary>Called once each time a swing is counted (the player
        /// leaned decisively to one side, then the other). Purely cosmetic,
        /// same as <see cref="ISoloGameVisual.SetProgress"/> — GameManager
        /// decides win/lose independently, not from this call.</summary>
        void Pulse();
    }

    /// <summary>
    /// Builds the visual for a given <see cref="SoloGameKind"/>. Looks for a
    /// hand-authored prefab first — drop your own art into
    /// <c>Assets/Resources/SoloVisuals/{KindName}.prefab</c> and it's picked
    /// up automatically, no code changes needed. The prefab doesn't need any
    /// script on it: if its root has a component implementing
    /// <see cref="ISoloGameVisual"/> already, that's used as-is; otherwise,
    /// for kinds with a known adapter (see <see cref="WrapPrefab"/>), one is
    /// attached at runtime that finds the pieces it needs (camera, moving
    /// parts) by name within the prefab's own hierarchy — so authoring a
    /// prefab is purely an art/scene-building task, no C# required. Falls
    /// back to a built-in placeholder for the kinds that have one; everything
    /// else keeps using GameManager's older procedural stage (plain colored
    /// UI rectangles).
    /// </summary>
    public static class SoloGameVisualFactory
    {
        public static ISoloGameVisual Create(SoloGameKind kind)
        {
            var prefab = Resources.Load<GameObject>($"SoloVisuals/{kind}");
            if (prefab != null)
            {
                var instance = Object.Instantiate(prefab);
                var visual = instance.GetComponent<ISoloGameVisual>() ?? WrapPrefab(kind, instance);
                if (visual != null) return visual;

                Debug.LogWarning($"[JesBox] Resources/SoloVisuals/{kind} has no component implementing ISoloGameVisual, and no built-in adapter recognizes this kind — using the built-in placeholder instead.");
                Object.Destroy(instance);
            }

            switch (kind)
            {
                case SoloGameKind.JoyfulPrayer:
                    return new GameObject($"{kind}Visual").AddComponent<JoyfulPrayerVisual>();
                case SoloGameKind.PartingTheSea:
                    return new GameObject($"{kind}Visual").AddComponent<PartingTheSeaVisual>();
                default:
                    return null;
            }
        }

        /// <summary>Attaches a kind-specific adapter directly onto an
        /// already-instantiated, script-free prefab. Add a new case here
        /// whenever you build a hand-authored scene for a kind that needs
        /// more than JoyfulPrayerVisual/PartingTheSeaVisual's simple
        /// "meter that fills" look.</summary>
        private static ISoloGameVisual WrapPrefab(SoloGameKind kind, GameObject instance)
        {
            switch (kind)
            {
                case SoloGameKind.PartingTheSea:
                    return instance.AddComponent<PartingTheSeaCustomVisual>();
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Placeholder 2D visual for Joyful Prayer: a soft glow that grows and
    /// warms up in color as the shake meter fills, procedurally generated so
    /// it works with no imported art at all. Assign <see cref="customSprite"/>
    /// in the Inspector (on a prefab under Resources/SoloVisuals, or directly
    /// on this component if you attach it by hand) to use your own painted
    /// art instead — everything else keeps working unchanged.
    /// </summary>
    public class JoyfulPrayerVisual : MonoBehaviour, ISoloGameVisual
    {
        [Tooltip("Your own 2D art. Leave empty to use the procedural placeholder glow.")]
        [SerializeField] private Sprite customSprite;

        private static Sprite _placeholderSprite;
        private Image _glow;
        private RectTransform _glowRt;
        private float _progress;

        public void Setup(RectTransform stage)
        {
            transform.SetParent(stage, false);

            var go = new GameObject("PrayerGlow", typeof(Image));
            _glowRt = go.GetComponent<RectTransform>();
            _glowRt.SetParent(transform, false);
            _glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            _glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            _glowRt.anchoredPosition = Vector2.zero;
            _glowRt.sizeDelta = new Vector2(220, 220);

            _glow = go.GetComponent<Image>();
            _glow.sprite = customSprite != null ? customSprite : GetPlaceholderSprite();
            _glow.raycastTarget = false;

            SetProgress(0f);
        }

        public void SetProgress(float fraction)
        {
            _progress = Mathf.Clamp01(fraction);
            if (_glow == null) return;
            _glow.color = Color.Lerp(new Color(0.55f, 0.4f, 0.75f, 0.7f), new Color(1f, 0.85f, 0.4f, 0.95f), _progress);
        }

        public void Teardown()
        {
            if (gameObject != null) Destroy(gameObject);
        }

        private void Update()
        {
            if (_glowRt == null) return;
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.05f;
            _glowRt.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.5f, _progress) * pulse;
            _glowRt.localRotation = Quaternion.Euler(0f, 0f, Time.time * 15f);
        }

        private static Sprite GetPlaceholderSprite()
        {
            if (_placeholderSprite != null) return _placeholderSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - dist), 1.6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _placeholderSprite;
        }
    }

    /// <summary>
    /// Placeholder 3D visual for Parting the Sea: a small low-poly-primitive
    /// diorama (a floor, two "water" walls that slide apart as progress
    /// increases, a light) rendered by a dedicated camera into a RenderTexture,
    /// which is what actually gets displayed on the stage — the rest of the
    /// game is a UI Canvas with no camera of its own, so this doesn't
    /// conflict with anything. Assign <see cref="customWaterWallPrefab"/> in
    /// the Inspector to swap in real low-poly meshes/materials for the walls.
    /// </summary>
    public class PartingTheSeaVisual : MonoBehaviour, ISoloGameVisual
    {
        [Tooltip("Your own low-poly wall mesh/prefab. Leave empty to use placeholder cubes.")]
        [SerializeField] private GameObject customWaterWallPrefab;

        private const int RenderWidth = 900;
        private const int RenderHeight = 420;

        private Camera _camera;
        private RenderTexture _renderTexture;
        private RawImage _display;
        private Transform _leftWall, _rightWall;
        private float _leftBaseY, _rightBaseY;
        private float _progress;

        public void Setup(RectTransform stage)
        {
            // This root stays in world space (it's a real 3D scene, not UI) —
            // parked far from the origin purely out of caution should any
            // other 3D content ever get added later. Only the RawImage that
            // displays its camera's RenderTexture becomes a stage child.
            transform.position = new Vector3(1000f, 0f, 0f);

            BuildDiorama();

            _renderTexture = new RenderTexture(RenderWidth, RenderHeight, 16) { name = "PartingTheSeaRT" };

            var camGo = new GameObject("PartingTheSeaCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 3.4f, -6f);
            camGo.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
            _camera = camGo.AddComponent<Camera>();
            _camera.targetTexture = _renderTexture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.05f, 0.2f, 0.28f);
            _camera.fieldOfView = 40f;

            var displayGo = new GameObject("PartingTheSeaDisplay", typeof(RawImage));
            var displayRt = displayGo.GetComponent<RectTransform>();
            displayRt.SetParent(stage, false);
            displayRt.anchorMin = Vector2.zero;
            displayRt.anchorMax = Vector2.one;
            displayRt.offsetMin = Vector2.zero;
            displayRt.offsetMax = Vector2.zero;
            _display = displayGo.GetComponent<RawImage>();
            _display.texture = _renderTexture;
            _display.raycastTarget = false;

            SetProgress(0f);
        }

        private void BuildDiorama()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = Vector3.one * 1.4f;
            ApplyColor(floor, new Color(0.75f, 0.7f, 0.45f));

            _leftWall = BuildWall(-1);
            _rightWall = BuildWall(1);
            _leftBaseY = _leftWall.localPosition.y;
            _rightBaseY = _rightWall.localPosition.y;

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }

        private Transform BuildWall(int side)
        {
            Transform wall;
            if (customWaterWallPrefab != null)
            {
                wall = Instantiate(customWaterWallPrefab, transform).transform;
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(transform, false);
                cube.transform.localScale = new Vector3(2.4f, 3f, 2.4f);
                ApplyColor(cube, new Color(0.15f, 0.45f, 0.65f, 0.9f));
                wall = cube.transform;
            }
            wall.localPosition = new Vector3(side * 1.6f, 1f, 0f);
            return wall;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            renderer.material = new Material(shader) { color = color };
        }

        public void SetProgress(float fraction)
        {
            _progress = Mathf.Clamp01(fraction);
            if (_leftWall == null || _rightWall == null) return;
            float gap = Mathf.Lerp(1.6f, 4.2f, _progress);
            _leftWall.localPosition = new Vector3(-gap, _leftWall.localPosition.y, 0f);
            _rightWall.localPosition = new Vector3(gap, _rightWall.localPosition.y, 0f);
        }

        public void Teardown()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
            if (_display != null) Destroy(_display.gameObject);
            if (gameObject != null) Destroy(gameObject);
        }

        private void Update()
        {
            if (_leftWall == null || _rightWall == null) return;
            float bob = Mathf.Sin(Time.time * 1.5f) * 0.08f;
            var lp = _leftWall.localPosition; lp.y = _leftBaseY + bob; _leftWall.localPosition = lp;
            var rp = _rightWall.localPosition; rp.y = _rightBaseY - bob; _rightWall.localPosition = rp;
        }
    }

    /// <summary>
    /// Adapter for a hand-authored SoloVisuals/PartingTheSea.prefab that has
    /// no script of its own — SoloGameVisualFactory attaches this straight
    /// onto the instantiated prefab at runtime. Finds a camera named
    /// "RenderCam", two objects named "WaterFront1"/"WaterFront2", and (if
    /// present) one named "Cone" anywhere in the prefab's hierarchy; redirects
    /// the camera to a RenderTexture (displayed via a RawImage on the stage,
    /// same as the placeholder), nudges the two water fronts apart as
    /// progress increases, and slides the cone left/right live as the chosen
    /// player tilts their phone. Everything else in the prefab (ground, sea
    /// surface, decoration, lighting) is left exactly as authored. If
    /// "RenderCam" isn't found, nothing is drawn — check the name matches
    /// exactly.
    /// </summary>
    public class PartingTheSeaCustomVisual : MonoBehaviour, ISteerableSoloGameVisual
    {
        // Matches SoloStageDefaultSize in GameManager.cs (Chosen One games
        // now fill nearly the whole screen) — upsized to match rather than
        // look blurry stretched.
        private const int RenderWidth = 1850;
        private const int RenderHeight = 850;
        // How far apart (world units) the water fronts end up at full
        // progress. Bumped up from the original 3 — at the authored camera
        // distance/FOV that was barely visible.
        private const float MaxSpread = 25f;
        // How far (world units) the cone bounces to the right and back on
        // each counted swing.
        private const float PulseDistance = 1f;
        private const float PulseOutTime = 0.08f;
        private const float PulseBackTime = 0.14f;

        private Camera _camera;
        private RenderTexture _renderTexture;
        private RawImage _display;
        private Transform _wallA, _wallB;
        private Vector3 _wallABase, _wallBBase;
        private Transform _cone;
        private Vector3 _coneBase;
        private Coroutine _pulseCoroutine;

        public void Setup(RectTransform stage)
        {
            _camera = FindDeep("RenderCam")?.GetComponent<Camera>();
            _wallA = FindDeep("WaterFront1");
            _wallB = FindDeep("WaterFront2");
            if (_wallA != null) _wallABase = _wallA.localPosition;
            if (_wallB != null) _wallBBase = _wallB.localPosition;

            // World position, not local — the cone's own parent/rotation in
            // the authored hierarchy isn't guaranteed to have local X line up
            // with true left/right the way the root-level water fronts do.
            _cone = FindDeep("Cone");
            if (_cone != null) _coneBase = _cone.position;

            if (_camera == null)
            {
                Debug.LogWarning("[JesBox] SoloVisuals/PartingTheSea.prefab has no object named \"RenderCam\" with a Camera — nothing will be displayed for this turn.");
                SetProgress(0f);
                return;
            }

            // This prefab's camera comes with its own AudioListener (needed
            // while authoring it standalone) — disable it here so it doesn't
            // fight whatever listener is already live in the actual game scene.
            var listener = _camera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;

            _renderTexture = new RenderTexture(RenderWidth, RenderHeight, 16) { name = "PartingTheSeaCustomRT" };
            _camera.targetTexture = _renderTexture;

            var displayGo = new GameObject("PartingTheSeaCustomDisplay", typeof(RawImage));
            var displayRt = displayGo.GetComponent<RectTransform>();
            displayRt.SetParent(stage, false);
            displayRt.anchorMin = Vector2.zero;
            displayRt.anchorMax = Vector2.one;
            displayRt.offsetMin = Vector2.zero;
            displayRt.offsetMax = Vector2.zero;
            _display = displayGo.GetComponent<RawImage>();
            _display.texture = _renderTexture;
            _display.raycastTarget = false;

            SetProgress(0f);
        }

        public void SetProgress(float fraction)
        {
            float spread = Mathf.Lerp(0f, MaxSpread, Mathf.Clamp01(fraction));
            if (_wallA != null) _wallA.localPosition = _wallABase + new Vector3(-spread, 0f, 0f);
            if (_wallB != null) _wallB.localPosition = _wallBBase + new Vector3(spread, 0f, 0f);
        }

        /// <summary>Bounces the cone one unit to the right (world space) and
        /// back to its starting spot — a quick, punchy hit of feedback each
        /// time a swing lands, rather than continuously tracking raw tilt.</summary>
        public void Pulse()
        {
            if (_cone == null) return;
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            Vector3 target = _coneBase + new Vector3(PulseDistance, 0f, 0f);

            float t = 0f;
            while (t < PulseOutTime)
            {
                t += Time.deltaTime;
                _cone.position = Vector3.Lerp(_coneBase, target, Mathf.Clamp01(t / PulseOutTime));
                yield return null;
            }
            _cone.position = target;

            t = 0f;
            while (t < PulseBackTime)
            {
                t += Time.deltaTime;
                _cone.position = Vector3.Lerp(target, _coneBase, Mathf.Clamp01(t / PulseBackTime));
                yield return null;
            }
            _cone.position = _coneBase;
            _pulseCoroutine = null;
        }

        public void Teardown()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
            if (_display != null) Destroy(_display.gameObject);
            if (gameObject != null) Destroy(gameObject);
        }

        private Transform FindDeep(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t;
            }
            return null;
        }
    }
}
