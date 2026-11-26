using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FamousGreatGame.Editor
{
    public static class ArenaSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/Arena.unity";
        private const string PrefabPath = "Assets/Game/Prefabs/ArenaPlayer.prefab";

        [MenuItem("Famous Great Game/Create or Open Arena")]
        public static void OpenOrCreateArena()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }

            EnsureFolder("Assets/Game", "Scenes");
            EnsureFolder("Assets/Game", "Prefabs");
            PlayerSettings.companyName = "Famous Great Project";
            PlayerSettings.productName = "Famous Great Project";
            PlayerSettings.runInBackground = true;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (playerPrefab == null)
            {
                var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "ArenaPlayer";
                player.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                Object.DestroyImmediate(player.GetComponent<Collider>());
                player.AddComponent<NetworkObject>();
                player.AddComponent<NetworkTransform>();
                player.AddComponent<ArenaPlayer>();
                playerPrefab = PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
                Object.DestroyImmediate(player);
            }

            var network = new GameObject("Arena Session");
            var transport = network.AddComponent<UnityTransport>();
            var manager = network.AddComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.ConnectionApproval = true;
            network.AddComponent<ArenaSession>();

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Arena Floor";
            floor.transform.localScale = Vector3.one * (ArenaPlayer.ArenaHalfSize / 5f);
            var floorMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.18f, 0.24f) };
            const string floorMaterialPath = "Assets/Game/Prefabs/ArenaFloor.mat";
            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(floorMaterialPath);
            if (existingMaterial == null)
            {
                AssetDatabase.CreateAsset(floorMaterial, floorMaterialPath);
                existingMaterial = floorMaterial;
            }
            else
                Object.DestroyImmediate(floorMaterial);
            floor.GetComponent<Renderer>().sharedMaterial = existingMaterial;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 15f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.08f, 0.12f);
            camera.transform.position = new Vector3(0f, 19f, -15f);
            camera.transform.LookAt(Vector3.zero);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            light.intensity = 1.2f;
            RenderSettings.ambientLight = Color.gray;

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException($"Could not save {ScenePath}");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Arena ready: {ScenePath}");
        }

        [MenuItem("Famous Great Game/Refresh Arena Visuals")]
        public static void RefreshArenaVisuals()
        {
            OpenOrCreateArena();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return;

            var camera = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
            if (camera == null)
                throw new IOException("The arena needs a Main Camera.");

            var existing = GameObject.Find("Arena Environment");
            if (existing != null)
                Object.DestroyImmediate(existing);
            var root = new GameObject("Arena Environment").transform;
            EnsureFolder("Assets/Game", "Environment");
            var frame = Surface("Frame", 0x233243, 0.45f);
            var dark = Surface("Inner Deck", 0x142332, 0.25f);
            var tileA = Surface("Deck Slate", 0x304656, 0.28f);
            var tileB = Surface("Deck Steel", 0x334b5b, 0.30f);
            var cyan = Surface("Ice Light", 0x3bd6dd, 0.25f, 1.1f);
            var amber = Surface("Amber Light", 0xffaf57, 0.25f, 0.85f);
            var paint = Surface("Deck Markings", 0x56818d, 0.2f, 0.15f);
            var distant = Surface("Distant Structures", 0x192838, 0.1f);

            Box(root, "Lower Hull", 0, -1.50f, 0, 22.5f, 1.5f, 22.5f, dark);
            Box(root, "Armored Rim", 0, -0.52f, 0, 22.0f, 0.75f, 22.0f, frame);
            Box(root, "Panel Substrate", 0, -0.12f, 0, 20.2f, 0.18f, 20.2f, dark);
            var deck = Group(root, "Modular Deck");
            for (int z = 0; z < 10; z++)
            for (int x = 0; x < 10; x++)
                Box(deck, "Deck Panel", -9f + 2f * x, -0.014f, -9f + 2f * z,
                    1.95f, 0.032f, 1.95f, (x + z) % 2 == 0 ? tileA : tileB);

            var markings = Group(root, "Lane Markings");
            for (int i = 0; i < 32; i++)
            {
                float angle = i * (360f / 32f);
                float rad = angle * Mathf.Deg2Rad;
                Box(markings, "Center Ring", 2.7f * Mathf.Sin(rad), 0.009f, 2.7f * Mathf.Cos(rad),
                    0.36f, 0.012f, 0.045f, paint, angle);
            }
            Box(markings, "Centerline", 0, 0.01f, 0, 0.65f, 0.012f, 0.055f, paint);
            Box(markings, "Centerline", 0, 0.01f, 0, 0.055f, 0.012f, 0.65f, paint);

            for (int side = 0; side < 4; side++)
            {
                var edge = Group(root, $"Perimeter {side + 1}");
                edge.localRotation = Quaternion.Euler(0, side * 90f, 0);
                Box(edge, "Inset Channel", 0, -0.07f, 10.26f, 19.0f, 0.16f, 0.43f, dark);
                Box(edge, "Boundary Light", 0, 0.019f, 10.22f, 17.2f, 0.035f, 0.095f, cyan);
                for (int i = -4; i <= 4; i++)
                {
                    Box(edge, "Rim Slat", i * 2f, -0.005f, 10.70f, 1.73f, 0.30f, 0.31f, frame);
                    Box(edge, "Lane Tick", i * 2f, 0.009f, 9.45f, 0.38f, 0.012f, 0.057f, paint);
                }
                Box(edge, "Corner Foundation", 10.35f, 0.04f, 10.35f, 1.10f, 0.48f, 1.10f, frame);
                Box(edge, "Corner Pylon", 10.35f, 0.57f, 10.35f, 0.72f, 0.80f, 0.72f, dark);
                Box(edge, "Amber Beacon", 10.35f, 1.00f, 10.35f, 0.54f, 0.095f, 0.54f, amber);
            }

            var skyline = Group(root, "Lower Service District");
            for (int i = 0; i < 13; i++)
            {
                float x = (i - 6) * 3.75f;
                float z = 14.8f + i % 3 * 3.2f;
                float height = 1.5f + i % 4 * 0.85f;
                float y = -4.8f + height * 0.5f;
                Box(skyline, "Service Stack", x, y, z, 2.7f, height, 2.8f, distant);
                Box(skyline, "Stack Cap", x, y + height * 0.5f, z, 2.9f, 0.20f, 2.9f, frame);
                Box(skyline, "Distant Signal", x, y + height * 0.5f - 0.22f, z - 1.41f,
                    1.35f, 0.08f, 0.04f, i % 4 == 0 ? amber : paint);
            }
            Box(skyline, "Distant Foundation", 0, -6.7f, 13, 55, 1.0f, 37, distant);

            camera.orthographicSize = 13.7f;
            camera.backgroundColor = Tint(0x101e30);
            camera.transform.position = new Vector3(0f, 20f, -15f);
            camera.transform.LookAt(new Vector3(0f, -0.15f, 0.55f));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Tint(0x8298ac);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 72f;
            var sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (sun != null)
            {
                sun.color = Tint(0xffe7c8);
                sun.intensity = 1.12f;
                sun.shadows = LightShadows.Soft;
            }
            var fill = Group(root, "Cool Fill").gameObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Tint(0x76bfff);
            fill.intensity = 0.33f;
            fill.transform.rotation = Quaternion.Euler(30f, 145f, 0f);

            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}");
            AssetDatabase.SaveAssets();
            Debug.Log("Arena visuals saved.");
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static void Box(Transform parent, string name, float x, float y, float z,
            float width, float height, float depth, Material material, float yaw = 0)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = new Vector3(x, y, z);
            box.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            box.transform.localScale = new Vector3(width, height, depth);
            box.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        private static Material Surface(string name, uint rgb, float smoothness, float emission = 0)
        {
            string path = $"Assets/Game/Environment/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = Tint(rgb);
            material.SetFloat("_Glossiness", smoothness);
            if (emission > 0)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", material.color * emission);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color Tint(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        public static void BuildMacSmokePlayer()
        {
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/ArenaSmoke.app",
                BuildTarget.StandaloneOSX, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new IOException($"Arena build failed: {report.summary.result}");
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
