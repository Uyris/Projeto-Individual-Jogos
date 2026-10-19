using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta os prefabs, a cena do jogo e as configurações do projeto a partir
/// dos assets gerados. Menu do editor: "Coletor Estelar > Reconstruir Jogo".
/// </summary>
public static class GameBuilder
{
    const string ArtDir = "Assets/Art/";
    const string AudioDir = "Assets/Audio/";
    const string PrefabDir = "Assets/Prefabs/";
    const string MaterialDir = "Assets/Materials/";
    const string SceneDir = "Assets/Scenes/";
    const string GameScenePath = SceneDir + "Game.unity";

    static readonly Color UiText = Color.white;
    static readonly Color UiMuted = new Color(0.6f, 0.7f, 0.85f);
    static readonly Color UiGold = new Color(1f, 0.82f, 0.3f);
    static readonly Color UiCyan = new Color(0.45f, 0.85f, 1f);

    static StringBuilder log;
    static Font pixelFont;
    static Font bodyFont;
    static Font titleFont;
    static PhysicsMaterial2D noFriction;

    static GameObject playerPrefab, crystalPrefab;

    [MenuItem("Coletor Estelar/Reconstruir Jogo")]
    public static void BuildFromMenu()
    {
        Debug.Log(BuildAll());
    }

    public static string BuildAll()
    {
        log = new StringBuilder();
        EnsureFolder(PrefabDir);
        EnsureFolder(MaterialDir);
        EnsureTags("Coletavel");
        ConfigureImporters();

        // por enquanto a fonte padrão do Unity
        pixelFont = bodyFont = titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateMaterials();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreatePrefabs();
        BuildGameScene();
        ConfigureProject();

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(GameScenePath);
        log.AppendLine("Pronto!");
        return log.ToString();
    }

    // ------------------------------------------------------------------ setup

    static void EnsureFolder(string path)
    {
        path = path.TrimEnd('/');
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void EnsureTags(params string[] tags)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty list = tagManager.FindProperty("tags");
        foreach (string tag in tags)
        {
            bool exists = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).stringValue == tag) exists = true;
            if (exists) continue;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = tag;
            log.AppendLine("Tag criada: " + tag);
        }
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigureImporters()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 32;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }

        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is AudioImporter ai)) continue;
            bool music = path.Contains("/Music/");
            AudioImporterSampleSettings s = ai.defaultSampleSettings;
            s.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = music ? 0.55f : 0.7f;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
        }
    }

    static Sprite Spr(string name) => Load<Sprite>(ArtDir + "Sprites/" + name + ".png");
    static AudioClip Sfx(string name) => Load<AudioClip>(AudioDir + "SFX/" + name + ".wav");

    static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) log.AppendLine("ASSET NÃO ENCONTRADO: " + path);
        return asset;
    }

    static void CreateMaterials()
    {
        string physicsPath = MaterialDir + "SemAtrito.physicsMaterial2D";
        noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physicsPath);
        if (noFriction == null)
        {
            noFriction = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(noFriction, physicsPath);
        }
    }

    // ------------------------------------------------------------------ helpers de cena

    static GameObject NewSprite(string name, Transform parent, Sprite sprite, int order, float scale = 1f)
    {
        var go = new GameObject(name);
        if (parent) go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        return go;
    }

    // Sprite básico (quadrado, círculo ou triângulo) com a cor trocada.
    static GameObject Shape(string name, Transform parent, string shape, Color color, int order, float scale = 1f)
    {
        GameObject go = NewSprite(name, parent, Spr(shape), order, scale);
        go.GetComponent<SpriteRenderer>().color = color;
        return go;
    }

    static GameObject SavePrefab(GameObject go)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + go.name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject Place(GameObject prefab, Vector2 position, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go;
    }

    // ------------------------------------------------------------------ prefabs

    static void CreatePrefabs()
    {
        // ---------- Jogador (círculo vermelho do tutorial)
        var player = new GameObject("Player") { tag = "Player" };
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.sharedMaterial = noFriction;
        player.AddComponent<CircleCollider2D>().radius = 0.36f;
        var audio = player.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        GameObject visual = Shape("Visual", player.transform, "circle", new Color(0.9f, 0.25f, 0.25f), 20, 0.75f);

        var pc = player.AddComponent<PlayerController>();
        pc.audioSource = audio;
        pc.collectClip = Sfx("collect");
        playerPrefab = SavePrefab(player);

        // ---------- Cristal (o triângulo coletável do tutorial)
        var crystal = new GameObject("Cristal") { tag = "Coletavel" };
        var poly = crystal.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.pathCount = 1;
        poly.SetPath(0, new[] { new Vector2(0f, 0.41f), new Vector2(-0.38f, -0.38f), new Vector2(0.38f, -0.38f) });
        GameObject crystalVisual = Shape("Visual", crystal.transform, "triangle", new Color(1f, 0.85f, 0.2f), 5);
        var cc = crystal.AddComponent<Collectible>();
        cc.points = 100;
        cc.visual = crystalVisual.transform;
        crystalPrefab = SavePrefab(crystal);

        log.AppendLine("Prefabs criados.");
    }

    // ------------------------------------------------------------------ cena do jogo

    static void BuildGameScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera(new Vector3(0f, 0.5f, -10f), 10.1f);

        // ---------- Arena
        Transform arena = new GameObject("Arena").transform;

        Transform walls = new GameObject("Paredes").transform;
        walls.SetParent(arena, false);
        CreateWall(walls, -17f, 8.5f, 34f, 1f);
        CreateWall(walls, -17f, -8.5f, 34f, 1f);
        CreateWall(walls, -17f, 7.5f, 1f, 16f);
        CreateWall(walls, 16f, 7.5f, 1f, 16f);

        // ---------- Objetos do mapa
        Transform items = new GameObject("Coletaveis").transform;
        Place(playerPrefab, Vector2.zero, null);

        // cristais em círculo ao redor do jogador, como no tutorial
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Place(crystalPrefab, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5f, items);
        }

        // ---------- Gerenciador e mensagem de vitória
        Canvas canvas = CreateCanvas("Canvas", 10);
        Text victory = AddText(canvas.transform, "MensagemVitoria", "VOCÊ VENCEU!", titleFont, 120, UiGold, TextAnchor.MiddleCenter,
                               new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 200f));

        var managers = new GameObject("GameManager");
        var gm = managers.AddComponent<GameManager>();
        gm.victoryText = victory.gameObject;

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), GameScenePath);
        log.AppendLine("Cena do jogo salva.");
    }

    static void CreateWall(Transform parent, float left, float top, float width, float height)
    {
        GameObject wall = Shape("Parede", parent, "square", new Color(0.6f, 0.6f, 0.65f), 0);
        wall.transform.position = new Vector3(left + width * 0.5f, top - height * 0.5f, 0f);
        var sr = wall.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, height);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(width, height);
    }

    static Camera CreateCamera(Vector3 position, float size)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = position;
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(8, 10, 22, 255);
        go.AddComponent<AudioListener>();
        return cam;
    }

    // ------------------------------------------------------------------ UI

    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static RectTransform Layout(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    static Text AddText(Transform parent, string name, string content, Font font, int size, Color color, TextAnchor align,
                        Vector2 anchor, Vector2 position, Vector2 box, Vector2? pivot = null, bool shadow = true)
    {
        RectTransform rt = Layout(NewUI(name, parent), anchor, position, box, pivot);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.text = content;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        if (shadow)
        {
            var s = rt.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.85f);
            s.effectDistance = new Vector2(size / 8f, -size / 8f);
        }
        return text;
    }

    static Canvas CreateCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // ------------------------------------------------------------------ projeto

    static void ConfigureProject()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        log.AppendLine("Build Settings atualizados.");
    }
}
