using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rise.Editor
{
    public static class RisePrototypeSetup
    {
        private const string ScenePath = "Assets/Scenes/RisePrototype.unity";
        private const string ArtPath = "Assets/Art/Rise/";

        [MenuItem("Rise/Open or Create Prototype")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }

            ImportArt();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera background = new GameObject("Portrait letterbox").AddComponent<Camera>();
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = Color.black;
            background.cullingMask = 0;
            background.depth = -10;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 16f / 3f;
            camera.backgroundColor = new Color32(115, 188, 207, 255);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 3.875f, -10f);
            cameraObject.AddComponent<AudioListener>();
            RiseCamera follow = cameraObject.AddComponent<RiseCamera>();

            RiseGame game = new GameObject("Run Controller").AddComponent<RiseGame>();
            WindSwipeInput input = game.gameObject.AddComponent<WindSwipeInput>();
            Assign(input, "gameCamera", camera);
            ConfigureWindGusts(game, camera);
            ConfigureBackgroundClouds(game, follow);

            Transform ground = new GameObject("Ground").transform;
            for (int x = 0; x < 6; x++)
                for (int y = 0; y < 3; y++)
                {
                    string tile = (x + y * 3) % 3 == 0 ? "grass1" : "grass2";
                    SpriteRenderer grass = SpriteObject("Grass", Sprite(tile), ground, -5);
                    grass.transform.position = new Vector3(x - 2.5f, y - 1.9f, 0f);
                }

            SpriteRenderer child = SpriteObject("Child", Sprite("child"), null, 1);
            child.transform.position = new Vector3(-0.625f, -0.1f, 0f);
            Transform anchor = new GameObject("String hand").transform;
            anchor.SetParent(child.transform, false);
            anchor.localPosition = new Vector3(0.25f, 0.25f, 0f);

            SpriteRenderer kiteArt = SpriteObject("Kite", Sprite("kite"), null, 5);
            kiteArt.transform.position = new Vector3(1.125f, 0.15f, 0f);
            Rigidbody2D body = kiteArt.gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0.32f;
            body.drag = 0.12f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CircleCollider2D kiteCollider = kiteArt.gameObject.AddComponent<CircleCollider2D>();
            kiteCollider.radius = 0.34f;
            KiteController kite = kiteArt.gameObject.AddComponent<KiteController>();
            SpriteRenderer shieldVisual = SpriteObject("Active shield", Sprite("kite-shield"), kite.transform, 4);
            shieldVisual.gameObject.SetActive(false);
            Assign(kite, "game", game);
            Assign(kite, "followCamera", follow);
            Assign(kite, "shieldVisual", shieldVisual.gameObject);

            LineRenderer line = new GameObject("Kite string").AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = line.endWidth = 1f / 16f;
            line.startColor = line.endColor = new Color32(199, 207, 204, 255);
            line.sortingOrder = 0;
            line.SetPosition(0, anchor.position);
            line.SetPosition(1, kite.transform.position);
            Material material = new Material(Shader.Find("Sprites/Default"));
            Directory.CreateDirectory("Assets/Art/Materials");
            AssetDatabase.CreateAsset(material, "Assets/Art/Materials/KiteString.mat");
            line.sharedMaterial = material;

            CourseSpawner course = new GameObject("Endless course").AddComponent<CourseSpawner>();
            Assign(course, "game", game);
            Assign(course, "followCamera", follow);
            Assign(course, "bubbleSprite", Sprite("bubble"));
            Assign(course, "shieldSprite", Sprite("shield"));
            SerializedObject courseData = new SerializedObject(course);
            SerializedProperty saws = courseData.FindProperty("sawSprites");
            saws.arraySize = 2;
            saws.GetArrayElementAtIndex(0).objectReferenceValue = Sprite("saw1");
            saws.GetArrayElementAtIndex(1).objectReferenceValue = Sprite("saw2");
            courseData.ApplyModifiedPropertiesWithoutUndo();

            RiseHud hud = CreateHud(camera);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Assign(game, "swipeInput", input);
            Assign(game, "kite", kite);
            Assign(game, "followCamera", follow);
            Assign(game, "course", course);
            Assign(game, "hud", hud);
            Assign(game, "kiteString", line);
            Assign(game, "stringAnchor", anchor);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.defaultIsNativeResolution = false;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(item => item.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = game.gameObject;
            Debug.Log("Rise prototype ready. Press Play, then drag upward in the Game view.");
        }

        private static void ImportArt()
        {
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(ArtPath, "*.png"))
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = true;
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                string name = Path.GetFileNameWithoutExtension(path);
                settings.spritePivot = name == "child" ? new Vector2(0.5f, 0f)
                    // The shield export adds one row below the original 16-pixel kite.
                    : name == "kite-shield" ? new Vector2(0.5f, 9f / 17f)
                    : name == "saw1" ? new Vector2(13f / 32f, 13f / 32f)
                    : name == "saw2" ? new Vector2(11f / 32f, 11f / 32f)
                    : new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + name + ".png");

        private static SpriteRenderer SpriteObject(string name, Sprite sprite, Transform parent, int order)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static RiseHud CreateHud(Camera camera)
        {
            GameObject root = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            RiseHud hud = root.AddComponent<RiseHud>();
            RectTransform viewport = Rect("Portrait HUD", root.transform);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;

            RectTransform scoreRect = Rect("Height", viewport);
            Position(scoreRect, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(200f, 28f));
            scoreRect.pivot = new Vector2(0f, 1f);
            PixelScore height = scoreRect.gameObject.AddComponent<PixelScore>();
            height.color = Color.white;
            height.Configure(3f, false);

            // Cover the complete Game view on loss, including desktop letterboxing.
            RectTransform panel = Rect("Black loss screen", root.transform);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            Image black = panel.gameObject.AddComponent<Image>();
            black.color = Color.black;
            RectTransform finalRect = Rect("Final height", panel);
            Position(finalRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 70f));
            PixelScore final = finalRect.gameObject.AddComponent<PixelScore>();
            final.color = Color.white;
            final.Configure(7f, true);
            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = black;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            panel.gameObject.SetActive(false);

            Assign(hud, "gameCamera", camera);
            Assign(hud, "canvas", canvas);
            Assign(hud, "viewport", viewport);
            Assign(hud, "height", height);
            Assign(hud, "endPanel", panel.gameObject);
            Assign(hud, "finalHeight", final);
            Assign(hud, "retry", button);
            return hud;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Position(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        [MenuItem("Rise/Apply Minimal Score UI")]
        public static void ApplyMinimalScoreUi()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            RiseGame game = Object.FindObjectOfType<RiseGame>();
            if (game == null) { Debug.LogWarning("Open the Rise prototype scene first."); return; }
            RiseHud oldHud = Object.FindObjectOfType<RiseHud>();
            if (oldHud != null) Undo.DestroyObjectImmediate(oldHud.gameObject);
            RiseHud hud = CreateHud(Camera.main);
            Undo.RegisterCreatedObjectUndo(hud.gameObject, "Apply minimal score UI");
            Assign(game, "hud", hud);
            foreach (Camera camera in Object.FindObjectsOfType<Camera>())
                if (camera.name == "Portrait letterbox") camera.backgroundColor = Color.black;
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
        }

        private static void Assign(Object target, string field, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWindGusts(RiseGame game, Camera camera)
        {
            WindGustEffect effect = Object.FindObjectOfType<WindGustEffect>();
            if (effect == null)
            {
                GameObject root = new GameObject("Wind gusts");
                // Camera-relative pixels keep the swipe's on-screen direction as the camera climbs.
                root.transform.SetParent(camera.transform, false);
                root.transform.localPosition = new Vector3(0f, 0f, 10f);
                effect = root.AddComponent<WindGustEffect>();
            }
            Assign(effect, "gameCamera", camera);
            Assign(effect, "windPixel", Sprite("wind"));
            Assign(game, "windGusts", effect);
        }

        private static void ConfigureBackgroundClouds(RiseGame game, RiseCamera camera)
        {
            BackgroundClouds clouds = Object.FindObjectOfType<BackgroundClouds>();
            if (clouds == null)
            {
                GameObject root = new GameObject("Background clouds");
                root.transform.SetParent(camera.transform, false);
                root.transform.localPosition = new Vector3(0f, 0f, 10f);
                clouds = root.AddComponent<BackgroundClouds>();
            }
            Assign(clouds, "followCamera", camera);
            SerializedObject data = new SerializedObject(clouds);
            SerializedProperty sprites = data.FindProperty("cloudSprites");
            sprites.arraySize = 3;
            for (int i = 0; i < sprites.arraySize; i++)
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = Sprite("cloud" + (i + 1));
            data.ApplyModifiedPropertiesWithoutUndo();
            Assign(game, "backgroundClouds", clouds);
        }

        [MenuItem("Rise/Add Background Clouds")]
        public static void AddBackgroundClouds()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            RiseGame game = Object.FindObjectOfType<RiseGame>();
            RiseCamera camera = Object.FindObjectOfType<RiseCamera>();
            if (game == null || camera == null) return;
            ConfigureBackgroundClouds(game, camera);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
        }

        [MenuItem("Rise/Add Wind Gusts")]
        public static void AddWindGusts()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            RiseGame game = Object.FindObjectOfType<RiseGame>();
            if (game == null || Camera.main == null) return;
            string path = ArtPath + "wind.png";
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            ConfigureWindGusts(game, Camera.main);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
        }

        [MenuItem("Rise/Preview Wind Gust")]
        public static void PreviewWindGust()
        {
            if (!EditorApplication.isPlaying) return;
            RiseGame game = Object.FindObjectOfType<RiseGame>();
            if (game == null) return;
            EditorApplication.isPaused = false;
            game.ResetRun();
            game.ApplySwipe(new Vector2(1f, 3.5f));
            double pauseAt = EditorApplication.timeSinceStartup + 0.15;
            void PausePreview()
            {
                if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup < pauseAt) return;
                EditorApplication.update -= PausePreview;
                if (EditorApplication.isPlaying) EditorApplication.isPaused = true;
            }
            EditorApplication.update += PausePreview;
        }

        [MenuItem("Rise/Toggle Loss Screen Preview")]
        public static void ToggleLossPreview()
        {
            if (!EditorApplication.isPlaying) return;
            RiseGame game = Object.FindObjectOfType<RiseGame>();
            if (game == null) return;
            if (game.State == RunState.GameOver) game.ResetRun();
            else
            {
                game.ApplySwipe(Vector2.up);
                game.EndRun("Preview");
            }
        }
    }
}
