using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Monta os prefabs, as cenas (MainMenu e Game) e as configurações do projeto a partir
/// dos assets gerados. Menu do editor: "Coletor Estelar > Reconstruir Jogo".
/// </summary>
public static class GameBuilder
{
    const string ArtDir = "Assets/Art/";
    const string AudioDir = "Assets/Audio/";
    const string PrefabDir = "Assets/Prefabs/";
    const string MaterialDir = "Assets/Materials/";
    const string SceneDir = "Assets/Scenes/";
    const string MenuScenePath = SceneDir + "MainMenu.unity";
    const string GameScenePath = SceneDir + "Game.unity";
    const string VolumeProfilePath = "Assets/Settings/GameVolumeProfile.asset";

    // Arena com 32 x 16 células de 1 unidade.
    // # parede | C cristal | O oxigênio | M mina | A alienígena | P jogador | X cápsula de fuga
    static readonly string[] Map =
    {
        "..C......#..........C.....#.....",
        ".........#................#..X..",
        "...O.....#..######..####..#.....",
        ".........#..#..........#..##..##",
        "####..####..#...C......#........",
        "............#.....A....#....C...",
        ".C..........#..........#........",
        ".....M......####..######..M.....",
        "..........................######",
        "######..###.....C...............",
        "........#...######..####..A.....",
        "..P.....#..............#........",
        "........#....M.........#..C..O..",
        "..C.........C.....######.....M..",
        "........#..........C............",
        "...O....#..C.................C..",
    };

    static readonly Color UiText = Color.white;
    static readonly Color UiMuted = new Color(0.6f, 0.7f, 0.85f);
    static readonly Color UiGold = new Color(1f, 0.82f, 0.3f);
    static readonly Color UiCyan = new Color(0.45f, 0.85f, 1f);

    static StringBuilder log;
    static Font pixelFont;
    static Font bodyFont;
    static Font titleFont;
    static Material fxMaterial;
    static Material trailMaterial;
    static PhysicsMaterial2D noFriction;
    static VolumeProfile volumeProfile;

    static GameObject playerPrefab, crystalPrefab, oxygenPrefab, dronePrefab, alienPrefab, minePrefab, podPrefab;
    static GameObject collectFxPrefab, hurtFxPrefab;

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
        EnsureTags("Coletavel", "Inimigo", "Parede", "Saida");
        ConfigureImporters();

        pixelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PressStart2P-Regular.ttf");
        bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/VT323-Regular.ttf");
        titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Jersey10-Regular.ttf");

        CreateMaterials();
        CreateVolumeProfile();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreatePrefabs();
        BuildGameScene();
        BuildMenuScene();
        ConfigureProject();

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuScenePath);
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

            bool ui = path.Contains("/UI/");
            bool background = path.Contains("/Backgrounds/");
            string file = Path.GetFileNameWithoutExtension(path);

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = background ? 24 : (ui ? 25 : 32);
            ti.filterMode = background ? FilterMode.Bilinear : FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = background ? TextureWrapMode.Mirror : TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(settings);

            switch (file)
            {
                case "panel": ti.spriteBorder = new Vector4(4, 4, 4, 4); break;
                case "button": ti.spriteBorder = new Vector4(4, 6, 4, 4); break;
                case "bar_frame": ti.spriteBorder = new Vector4(2, 2, 2, 2); break;
                default: ti.spriteBorder = Vector4.zero; break;
            }
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
    static Sprite UiSpr(string name) => Load<Sprite>(ArtDir + "UI/" + name + ".png");
    static AudioClip Sfx(string name) => Load<AudioClip>(AudioDir + "SFX/" + name + ".wav");
    static AudioClip Music(string name) => Load<AudioClip>(AudioDir + "Music/" + name + ".wav");

    static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) log.AppendLine("ASSET NÃO ENCONTRADO: " + path);
        return asset;
    }

    static void CreateMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        fxMaterial = CreateMaterial("FX_Particulas", shader, Load<Texture2D>(ArtDir + "Sprites/spark.png"));
        trailMaterial = CreateMaterial("FX_Rastro", shader, Load<Texture2D>(ArtDir + "Sprites/square.png"));

        string physicsPath = MaterialDir + "SemAtrito.physicsMaterial2D";
        noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physicsPath);
        if (noFriction == null)
        {
            noFriction = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(noFriction, physicsPath);
        }
    }

    static Material CreateMaterial(string name, Shader shader, Texture texture)
    {
        string path = MaterialDir + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetTexture("_MainTex", texture);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void CreateVolumeProfile()
    {
        AssetDatabase.DeleteAsset(VolumeProfilePath);
        volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(volumeProfile, VolumeProfilePath);

        Bloom bloom = volumeProfile.Add<Bloom>(true);
        bloom.threshold.Override(0.75f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.65f);

        Vignette vignette = volumeProfile.Add<Vignette>(true);
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.5f);

        foreach (VolumeComponent component in volumeProfile.components)
        {
            component.name = component.GetType().Name;
            AssetDatabase.AddObjectToAsset(component, volumeProfile);
        }
        EditorUtility.SetDirty(volumeProfile);
    }

    // ------------------------------------------------------------------ helpers de cena

    static Vector2 Cell(int c, int r) => new Vector2(c - 15.5f, 7f - r);

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

    static GameObject AddShadow(Transform parent, float size, int order)
    {
        GameObject shadow = NewSprite("Sombra", parent, Spr("circle"), order);
        shadow.transform.localPosition = new Vector3(0.08f, -0.14f, 0f);
        shadow.transform.localScale = new Vector3(size, size * 0.8f, 1f);
        shadow.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.35f);
        return shadow;
    }

    static Light2D AddPointLight(GameObject go, Color color, float radius, float intensity)
    {
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightInnerRadius = 0f;
        light.pointLightOuterRadius = radius;
        light.falloffIntensity = 0.65f;
        ApplyToAllSortingLayers(light);
        return light;
    }

    static void ApplyToAllSortingLayers(Light2D light)
    {
        var so = new SerializedObject(light);
        SerializedProperty prop = so.FindProperty("m_ApplyToSortingLayers");
        if (prop == null) return;
        SortingLayer[] layers = SortingLayer.layers;
        prop.arraySize = layers.Length;
        for (int i = 0; i < layers.Length; i++) prop.GetArrayElementAtIndex(i).intValue = layers[i].id;
        so.ApplyModifiedPropertiesWithoutUndo();
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

    static GameObject BuildBurstFx(string name, int count, float speedMin, float speedMax, float sizeMin, float sizeMax, float lifetime)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = 100;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 100f;
        limit.drag = 3f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = fxMaterial;
        renderer.sortingOrder = 40;
        return go;
    }

    static void CreatePrefabs()
    {
        collectFxPrefab = SavePrefab(BuildBurstFx("FX_Coleta", 16, 2f, 5f, 0.12f, 0.24f, 0.55f));
        hurtFxPrefab = SavePrefab(BuildBurstFx("FX_Dano", 22, 3f, 7f, 0.14f, 0.3f, 0.6f));

        // ---------- Jogador (círculo vermelho do tutorial virou um astronauta)
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

        AddShadow(player.transform, 0.8f, 18);
        GameObject visual = NewSprite("Visual", player.transform, Spr("player"), 20);

        var trail = player.AddComponent<TrailRenderer>();
        trail.time = 0.22f;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = AnimationCurve.Linear(0f, 0.6f, 1f, 0f);
        trail.sharedMaterial = trailMaterial;
        trail.sortingOrder = 15;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.45f, 0.35f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.4f), 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
        trail.emitting = false;

        var lamp = new GameObject("Lanterna");
        lamp.transform.SetParent(player.transform, false);
        AddPointLight(lamp, new Color(1f, 0.95f, 0.85f), 3.5f, 0.55f);

        var pc = player.AddComponent<PlayerController>();
        pc.visual = visual.transform;
        pc.body = visual.GetComponent<SpriteRenderer>();
        pc.dashTrail = trail;
        pc.audioSource = audio;
        pc.collectClip = Sfx("collect");
        pc.oxygenClip = Sfx("oxygen");
        pc.hurtClip = Sfx("hurt");
        pc.dashClip = Sfx("dash");
        pc.collectFx = collectFxPrefab.GetComponent<ParticleSystem>();
        pc.hurtFx = hurtFxPrefab.GetComponent<ParticleSystem>();
        playerPrefab = SavePrefab(player);

        // ---------- Cristal (o triângulo coletável do tutorial)
        var crystal = new GameObject("Cristal") { tag = "Coletavel" };
        var poly = crystal.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.pathCount = 1;
        poly.SetPath(0, new[] { new Vector2(0f, 0.41f), new Vector2(-0.38f, -0.38f), new Vector2(0.38f, -0.38f) });
        GameObject crystalVisual = NewSprite("Visual", crystal.transform, Spr("crystal"), 5);
        var cc = crystal.AddComponent<Collectible>();
        cc.kind = Collectible.Kind.Crystal;
        cc.points = 100;
        cc.oxygen = 3f;
        cc.fxColor = new Color(0.5f, 0.95f, 1f);
        cc.visual = crystalVisual.transform;
        AddPointLight(crystal, new Color(0.35f, 0.9f, 1f), 1.8f, 0.9f);
        crystalPrefab = SavePrefab(crystal);

        // ---------- Tanque de oxigênio
        var tank = new GameObject("TanqueOxigenio") { tag = "Coletavel" };
        var box = tank.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.5f, 0.8f);
        GameObject tankVisual = NewSprite("Visual", tank.transform, Spr("oxygen_tank"), 5);
        var tc = tank.AddComponent<Collectible>();
        tc.kind = Collectible.Kind.Oxygen;
        tc.points = 50;
        tc.oxygen = 10f;
        tc.fxColor = new Color(0.4f, 1f, 0.55f);
        tc.visual = tankVisual.transform;
        AddPointLight(tank, new Color(0.4f, 1f, 0.55f), 1.4f, 0.6f);
        oxygenPrefab = SavePrefab(tank);

        // ---------- Drone de patrulha
        var drone = new GameObject("Drone") { tag = "Inimigo" };
        var drb = drone.AddComponent<Rigidbody2D>();
        drb.bodyType = RigidbodyType2D.Kinematic;
        drb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var dcol = drone.AddComponent<CircleCollider2D>();
        dcol.isTrigger = true;
        dcol.radius = 0.4f;
        AddShadow(drone.transform, 0.75f, 9);
        GameObject droneVisual = NewSprite("Visual", drone.transform, Spr("drone"), 10);
        droneVisual.AddComponent<Floater>().rotateSpeed = 140f;
        drone.AddComponent<PatrolEnemy>();
        AddPointLight(drone, new Color(1f, 0.3f, 0.25f), 1.5f, 0.7f);
        dronePrefab = SavePrefab(drone);

        // ---------- Alienígena perseguidor
        var alien = new GameObject("Alienigena");
        var arb = alien.AddComponent<Rigidbody2D>();
        arb.gravityScale = 0f;
        arb.freezeRotation = true;
        arb.interpolation = RigidbodyInterpolation2D.Interpolate;
        arb.sharedMaterial = noFriction;
        var bodyCollider = alien.AddComponent<CircleCollider2D>();
        bodyCollider.radius = 0.42f;
        var hitbox = new GameObject("Hitbox") { tag = "Inimigo" };
        hitbox.transform.SetParent(alien.transform, false);
        var hcol = hitbox.AddComponent<CircleCollider2D>();
        hcol.isTrigger = true;
        hcol.radius = 0.4f;
        AddShadow(alien.transform, 0.85f, 10);
        GameObject alienVisual = NewSprite("Visual", alien.transform, Spr("alien"), 11, 1.1f);
        GameObject alert = NewSprite("Alerta", alien.transform, Spr("alert"), 30);
        alert.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        var chaser = alien.AddComponent<ChaserEnemy>();
        chaser.bodyCollider = bodyCollider;
        chaser.body = alienVisual.GetComponent<SpriteRenderer>();
        chaser.alertIcon = alert;
        chaser.alertClip = Sfx("alert");
        AddPointLight(alien, new Color(0.5f, 1f, 0.45f), 1.6f, 0.5f);
        alienPrefab = SavePrefab(alien);

        // ---------- Mina (inimigo parado)
        var mine = new GameObject("Mina") { tag = "Inimigo" };
        var mcol = mine.AddComponent<CircleCollider2D>();
        mcol.isTrigger = true;
        mcol.radius = 0.36f;
        GameObject mineVisual = NewSprite("Visual", mine.transform, Spr("mine"), 8);
        mineVisual.AddComponent<Floater>().rotateSpeed = 45f;
        AddPointLight(mine, new Color(1f, 0.25f, 0.25f), 1.2f, 0.8f);
        minePrefab = SavePrefab(mine);

        // ---------- Cápsula de fuga
        var pod = new GameObject("CapsulaDeFuga") { tag = "Saida" };
        var pcol = pod.AddComponent<CircleCollider2D>();
        pcol.isTrigger = true;
        pcol.radius = 0.5f;
        GameObject podVisual = NewSprite("Visual", pod.transform, Spr("pod_locked"), 4);
        GameObject openFx = BuildBurstFx("FX_Abertura", 40, 3f, 7f, 0.15f, 0.3f, 0.9f);
        openFx.transform.SetParent(pod.transform, false);
        var openPs = openFx.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule openMain = openPs.main;
        openMain.playOnAwake = false;
        openMain.stopAction = ParticleSystemStopAction.None;
        openMain.startColor = new Color(0.4f, 1f, 0.6f);
        var exit = pod.AddComponent<ExitPod>();
        exit.body = podVisual.GetComponent<SpriteRenderer>();
        exit.lockedSprite = Spr("pod_locked");
        exit.openSprite = Spr("pod_open");
        exit.glow = AddPointLight(pod, new Color(1f, 0.25f, 0.3f), 3f, 0.6f);
        exit.openFx = openPs;
        podPrefab = SavePrefab(pod);

        log.AppendLine("Prefabs criados.");
    }

    // ------------------------------------------------------------------ cena do jogo

    static void BuildGameScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam = CreateCamera(new Vector3(0f, 0.5f, -10f), 10.1f);
        cam.gameObject.AddComponent<CameraController>();

        var globalLight = new GameObject("Global Light 2D").AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 0.8f;
        globalLight.color = new Color(0.82f, 0.88f, 1f);
        ApplyToAllSortingLayers(globalLight);

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = volumeProfile;

        GameObject background = NewSprite("FundoEspacial", null, Load<Sprite>(ArtDir + "Backgrounds/space.png"), -100, 1.3f);
        background.transform.position = new Vector3(0f, 0.5f, 0f);

        // ---------- Arena
        Transform arena = new GameObject("Arena").transform;
        GameObject floor = NewSprite("Piso", arena, Spr("floor"), -10);
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        var floorRenderer = floor.GetComponent<SpriteRenderer>();
        floorRenderer.drawMode = SpriteDrawMode.Tiled;
        floorRenderer.size = new Vector2(32f, 16f);

        // zonas pintadas no chão (sprite básico com a cor trocada)
        CreateZone(arena, "ZonaInicial", new Vector2(-12f, -5.5f), new Vector2(8f, 6f), new Color(0.3f, 0.6f, 1f, 0.07f));
        CreateZone(arena, "ZonaDeFuga", new Vector2(13.5f, 6f), new Vector2(5f, 3f), new Color(0.3f, 1f, 0.5f, 0.07f));

        Transform walls = new GameObject("Paredes").transform;
        walls.SetParent(arena, false);
        CreateWall(walls, -17f, 8.5f, 34f, 1f);
        CreateWall(walls, -17f, -8.5f, 34f, 1f);
        CreateWall(walls, -17f, 7.5f, 1f, 16f);
        CreateWall(walls, 16f, 7.5f, 1f, 16f);
        int wallCount = BuildMapWalls(walls);

        // ---------- Objetos do mapa
        Transform items = new GameObject("Coletaveis").transform;
        Transform enemies = new GameObject("Inimigos").transform;
        GameObject playerGO = null;
        GameObject podGO = null;
        var alienPatrols = new List<Vector2[]>
        {
            new[] { new Vector2(-1.5f, 3.5f), new Vector2(5.5f, 3.5f), new Vector2(5.5f, 1.5f), new Vector2(-1.5f, 1.5f) },
            new[] { new Vector2(9.5f, -3f), new Vector2(14.5f, -3f), new Vector2(14.5f, -7f), new Vector2(9.5f, -7f) },
        };
        int alienIndex = 0;

        for (int r = 0; r < Map.Length; r++)
        {
            for (int c = 0; c < Map[r].Length; c++)
            {
                Vector2 p = Cell(c, r);
                switch (Map[r][c])
                {
                    case 'C': Place(crystalPrefab, p, items); break;
                    case 'O': Place(oxygenPrefab, p, items); break;
                    case 'M': Place(minePrefab, p, enemies); break;
                    case 'P': playerGO = Place(playerPrefab, p, null); break;
                    case 'X': podGO = Place(podPrefab, p, null); break;
                    case 'A':
                        GameObject alien = Place(alienPrefab, p, enemies);
                        alien.GetComponent<ChaserEnemy>().patrolPoints = alienPatrols[alienIndex++ % alienPatrols.Count];
                        break;
                }
            }
        }

        // drones com rotas pré-determinadas
        CreateDrone(enemies, 3f, true, new Vector2(-15f, 5.5f), new Vector2(-7.6f, 5.5f));
        CreateDrone(enemies, 3.2f, true, new Vector2(-5f, 6.5f), new Vector2(-5f, 0f));
        CreateDrone(enemies, 3.5f, true, new Vector2(-15f, -1f), new Vector2(9f, -1f));
        CreateDrone(enemies, 3f, true, new Vector2(-6.5f, -7f), new Vector2(8f, -7f));
        CreateDrone(enemies, 2.8f, false, new Vector2(9f, 3f), new Vector2(15f, 3f), new Vector2(15f, 1f), new Vector2(9f, 1f));

        // lasers temporizados
        Transform lasers = new GameObject("Lasers").transform;
        CreateLaser(lasers, new Vector2(-1.5f, 7.5f), new Vector2(-1.5f, 5.5f), 0f);
        CreateLaser(lasers, new Vector2(5.5f, 7.5f), new Vector2(5.5f, 5.5f), 1.7f);
        CreateLaser(lasers, new Vector2(12f, 4f), new Vector2(14f, 4f), 0.9f);
        CreateLaser(lasers, new Vector2(0f, 0f), new Vector2(2f, 0f), 0.4f);

        // ---------- Gerenciadores e HUD
        HUD hud = BuildHud();
        CreateEventSystem();

        var managers = new GameObject("GameManager");
        var gm = managers.AddComponent<GameManager>();
        gm.player = playerGO.GetComponent<PlayerController>();
        gm.exitPod = podGO.GetComponent<ExitPod>();
        gm.hud = hud;
        gm.countdownClip = Sfx("countdown");
        gm.goClip = Sfx("go");
        gm.unlockClip = Sfx("unlock");
        gm.alarmClip = Sfx("alarm");
        gm.victoryClip = Sfx("victory");
        gm.gameOverClip = Sfx("gameover");
        AddAudioManager(managers, Music("music_game"), 0.4f);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), GameScenePath);
        log.AppendLine($"Cena do jogo salva ({wallCount} blocos de parede internos).");
    }

    static void CreateZone(Transform parent, string name, Vector2 center, Vector2 size, Color color)
    {
        GameObject zone = NewSprite(name, parent, Spr("square"), -9);
        zone.transform.position = center;
        zone.transform.localScale = new Vector3(size.x, size.y, 1f);
        zone.GetComponent<SpriteRenderer>().color = color;
    }

    static void CreateWall(Transform parent, float left, float top, float width, float height)
    {
        GameObject wall = NewSprite("Parede", parent, Spr("wall"), 0);
        wall.tag = "Parede";
        wall.transform.position = new Vector3(left + width * 0.5f, top - height * 0.5f, 0f);
        var sr = wall.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, height);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(width, height);
    }

    /// <summary>Junta as células '#' vizinhas em retângulos para usar menos colliders.</summary>
    static int BuildMapWalls(Transform parent)
    {
        int rows = Map.Length, cols = Map[0].Length, count = 0;
        var used = new bool[rows, cols];
        bool IsFree(int r, int c) => Map[r][c] == '#' && !used[r, c];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!IsFree(r, c)) continue;
                int w = 1;
                while (c + w < cols && IsFree(r, c + w)) w++;
                int h = 1;
                while (r + h < rows)
                {
                    bool fullRow = true;
                    for (int k = 0; k < w; k++) fullRow &= IsFree(r + h, c + k);
                    if (!fullRow) break;
                    h++;
                }
                for (int dr = 0; dr < h; dr++)
                    for (int dc = 0; dc < w; dc++)
                        used[r + dr, c + dc] = true;

                CreateWall(parent, c - 16f, 7.5f - r, w, h);
                count++;
            }
        }
        return count;
    }

    static void CreateDrone(Transform parent, float speed, bool pingPong, params Vector2[] waypoints)
    {
        GameObject drone = Place(dronePrefab, waypoints[0], parent);
        var patrol = drone.GetComponent<PatrolEnemy>();
        patrol.waypoints = waypoints;
        patrol.speed = speed;
        patrol.pingPong = pingPong;
    }

    static void CreateLaser(Transform parent, Vector2 a, Vector2 b, float offset)
    {
        var root = new GameObject("Laser");
        root.transform.SetParent(parent, false);
        root.transform.position = (a + b) * 0.5f;

        Vector2 d = b - a;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

        GameObject beam = NewSprite("Feixe", root.transform, Spr("square"), 7);
        beam.tag = "Inimigo";
        beam.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        beam.transform.localScale = new Vector3(d.magnitude, 0.22f, 1f);
        var beamRenderer = beam.GetComponent<SpriteRenderer>();
        beamRenderer.color = new Color(1f, 0.2f, 0.28f);
        var beamCollider = beam.AddComponent<BoxCollider2D>();
        beamCollider.isTrigger = true;
        beamCollider.size = new Vector2(1f, 1.4f);

        GameObject e1 = NewSprite("EmissorA", root.transform, Spr("laser_emitter"), 8);
        e1.transform.position = a;
        e1.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        GameObject e2 = NewSprite("EmissorB", root.transform, Spr("laser_emitter"), 8);
        e2.transform.position = b;
        e2.transform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);

        var gate = root.AddComponent<LaserGate>();
        gate.beam = beamRenderer;
        gate.beamCollider = beamCollider;
        gate.glow = AddPointLight(root, new Color(1f, 0.25f, 0.3f), d.magnitude * 0.6f + 0.8f, 1.2f);
        gate.timeOffset = offset;
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
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        return cam;
    }

    static void AddAudioManager(GameObject go, AudioClip music, float volume)
    {
        var am = go.AddComponent<AudioManager>();
        am.musicSource = go.AddComponent<AudioSource>();
        am.musicSource.playOnAwake = false;
        am.sfxSource = go.AddComponent<AudioSource>();
        am.sfxSource.playOnAwake = false;
        am.music = music;
        am.musicVolume = volume;
        am.clickClip = Sfx("click");
        am.selectClip = Sfx("select");
    }

    static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem", typeof(EventSystem));
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
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

    static RectTransform Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
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

    static Image AddImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 position,
                          Vector2 size, bool sliced = false, Vector2? pivot = null)
    {
        RectTransform rt = Layout(NewUI(name, parent), anchor, position, size, pivot);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sliced) image.type = Image.Type.Sliced;
        return image;
    }

    static Button AddButton(Transform parent, string name, string label, Vector2 position, Vector2 size, int fontSize = 48)
    {
        Image image = AddImage(parent, name, UiSpr("button"), Color.white, new Vector2(0.5f, 0.5f), position, size, true);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.3f, 0.55f);
        colors.highlightedColor = new Color(1f, 0.6f, 0.18f);
        colors.selectedColor = new Color(1f, 0.6f, 0.18f);
        colors.pressedColor = new Color(0.8f, 0.42f, 0.1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.35f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        Text text = AddText(image.transform, "Texto", label, titleFont, fontSize, Color.white, TextAnchor.MiddleCenter,
                            new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), size);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        image.gameObject.AddComponent<UIButtonSound>();
        return button;
    }

    static (GameObject root, RectTransform window) AddPanel(Transform parent, string name, string title, Color titleColor, Vector2 size)
    {
        RectTransform root = Stretch(NewUI(name, parent));
        var dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0.02f, 0.72f);
        dim.raycastTarget = true;

        Image window = AddImage(root, "Janela", UiSpr("panel"), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, size, true);
        AddText(window.transform, "Titulo", title, titleFont, 80, titleColor, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(size.x - 40f, 80f));
        return (root.gameObject, window.rectTransform);
    }

    static (RectTransform fill, Image fillImage) AddBar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        Image frame = AddImage(parent, name, UiSpr("bar_frame"), Color.white, anchor, position, size, true, new Vector2(1f, 0.5f));
        RectTransform area = Stretch(NewUI("Area", frame.transform), 8f);
        RectTransform fill = NewUI("Preenchimento", area);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.sprite = UiSpr("white");
        fillImage.color = color;
        fillImage.raycastTarget = false;
        return (fill, fillImage);
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

    static HUD BuildHud()
    {
        Canvas canvas = CreateCanvas("HUD", 10);
        var hud = canvas.gameObject.AddComponent<HUD>();
        Transform root = canvas.transform;
        Vector2 left = new Vector2(0f, 0.5f), center = new Vector2(0.5f, 0.5f), right = new Vector2(1f, 0.5f);

        // ---------- barra superior
        Image bar = AddImage(root, "BarraSuperior", UiSpr("white"), new Color(0.03f, 0.04f, 0.09f, 0.85f),
                             new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 112f), false, new Vector2(0.5f, 1f));
        bar.rectTransform.anchorMin = new Vector2(0f, 1f);
        bar.rectTransform.anchorMax = new Vector2(1f, 1f);
        Image line = AddImage(bar.transform, "Linha", UiSpr("white"), new Color(0.35f, 0.8f, 1f, 0.6f),
                              new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 4f), false, new Vector2(0.5f, 0f));
        line.rectTransform.anchorMin = new Vector2(0f, 0f);
        line.rectTransform.anchorMax = new Vector2(1f, 0f);
        Transform b = bar.transform;

        AddImage(b, "IconeCristal", UiSpr("icon_crystal"), Color.white, left, new Vector2(60f, 0f), new Vector2(56f, 56f));
        hud.crystalText = AddText(b, "Cristais", "0/12", pixelFont, 32, UiCyan, TextAnchor.MiddleLeft,
                                  left, new Vector2(100f, -2f), new Vector2(200f, 60f), new Vector2(0f, 0.5f));
        AddText(b, "RotuloPontos", "PONTOS", pixelFont, 16, UiMuted, TextAnchor.MiddleLeft,
                left, new Vector2(310f, 24f), new Vector2(240f, 30f), new Vector2(0f, 0.5f));
        hud.scoreText = AddText(b, "Pontos", "00000", pixelFont, 32, UiGold, TextAnchor.MiddleLeft,
                                left, new Vector2(310f, -16f), new Vector2(240f, 50f), new Vector2(0f, 0.5f));

        AddText(b, "RotuloTempo", "TEMPO", pixelFont, 16, UiMuted, TextAnchor.MiddleCenter,
                center, new Vector2(0f, 26f), new Vector2(300f, 30f));
        AddImage(b, "IconeRelogio", UiSpr("icon_clock"), Color.white, center, new Vector2(-196f, -14f), new Vector2(44f, 44f));
        hud.timeText = AddText(b, "Tempo", "00:00.00", pixelFont, 40, UiText, TextAnchor.MiddleCenter,
                               center, new Vector2(16f, -14f), new Vector2(380f, 56f));

        hud.hearts = new Image[3];
        for (int i = 0; i < 3; i++)
            hud.hearts[i] = AddImage(b, "Coracao" + i, UiSpr("heart_full"), Color.white, right,
                                     new Vector2(-188f + i * 64f, 0f), new Vector2(52f, 52f));
        hud.heartFull = UiSpr("heart_full");
        hud.heartEmpty = UiSpr("heart_empty");

        AddImage(b, "IconeOxigenio", UiSpr("icon_oxygen"), Color.white, right, new Vector2(-616f, 20f), new Vector2(36f, 36f));
        (hud.oxygenFill, hud.oxygenFillImage) = AddBar(b, "BarraOxigenio", right, new Vector2(-250f, 20f), new Vector2(330f, 34f), UiCyan);
        hud.oxygenText = AddText(hud.oxygenFill.parent.parent, "TextoOxigenio", "60s", pixelFont, 16, UiText, TextAnchor.MiddleCenter,
                                 center, new Vector2(0f, 0f), new Vector2(320f, 30f));

        AddImage(b, "IconeStamina", UiSpr("icon_stamina"), Color.white, right, new Vector2(-616f, -24f), new Vector2(32f, 32f));
        (hud.staminaFill, hud.staminaFillImage) = AddBar(b, "BarraStamina", right, new Vector2(-250f, -24f), new Vector2(330f, 24f), UiGold);

        // ---------- mensagens
        hud.messageText = AddText(root, "Mensagem", "", titleFont, 60, new Color(1f, 0.9f, 0.45f), TextAnchor.MiddleCenter,
                                  new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1600f, 160f));
        hud.messageText.lineSpacing = 0.9f;
        var outline = hud.messageText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        hud.countdownText = AddText(root, "Contagem", "3", pixelFont, 120, Color.white, TextAnchor.MiddleCenter,
                                    center, new Vector2(0f, 40f), new Vector2(900f, 220f));

        // ---------- pausa
        var (pauseRoot, pauseWindow) = AddPanel(root, "PainelPausa", "PAUSADO", Color.white, new Vector2(900f, 680f));
        AddText(pauseWindow, "Dicas",
                "<color=#ffd23f>MOVER</color>  WASD / SETAS / ANALÓGICO\n<color=#ffd23f>DASH</color>   ESPAÇO / SHIFT / A / RT\n<color=#ffd23f>PAUSA</color>  ESC / P / START",
                bodyFont, 40, UiText, TextAnchor.MiddleCenter, center, new Vector2(0f, 130f), new Vector2(820f, 160f));
        hud.resumeButton = AddButton(pauseWindow, "BotaoContinuar", "CONTINUAR", new Vector2(0f, -40f), new Vector2(520f, 76f));
        hud.pauseRestartButton = AddButton(pauseWindow, "BotaoReiniciar", "REINICIAR", new Vector2(0f, -136f), new Vector2(520f, 76f));
        hud.pauseMenuButton = AddButton(pauseWindow, "BotaoMenu", "MENU PRINCIPAL", new Vector2(0f, -232f), new Vector2(520f, 76f));
        pauseRoot.AddComponent<MenuFocus>().firstSelected = hud.resumeButton.gameObject;
        hud.pausePanel = pauseRoot;

        // ---------- derrota
        var (overRoot, overWindow) = AddPanel(root, "PainelDerrota", "FIM DE JOGO", new Color(1f, 0.35f, 0.35f), new Vector2(900f, 680f));
        hud.gameOverReasonText = AddText(overWindow, "Motivo", "", titleFont, 52, new Color(1f, 0.7f, 0.4f), TextAnchor.MiddleCenter,
                                         center, new Vector2(0f, 180f), new Vector2(820f, 50f));
        hud.gameOverStatsText = AddText(overWindow, "Resumo", "", titleFont, 48, UiText, TextAnchor.MiddleCenter,
                                        center, new Vector2(0f, 50f), new Vector2(560f, 160f));
        hud.gameOverStatsText.lineSpacing = 1f;
        hud.gameOverRestartButton = AddButton(overWindow, "BotaoTentar", "TENTAR DE NOVO", new Vector2(0f, -150f), new Vector2(520f, 76f));
        hud.gameOverMenuButton = AddButton(overWindow, "BotaoMenu", "MENU PRINCIPAL", new Vector2(0f, -246f), new Vector2(520f, 76f));
        overRoot.AddComponent<MenuFocus>().firstSelected = hud.gameOverRestartButton.gameObject;
        hud.gameOverPanel = overRoot;

        // ---------- vitória
        var (winRoot, winWindow) = AddPanel(root, "PainelVitoria", "VOCÊ ESCAPOU!", new Color(0.45f, 1f, 0.6f), new Vector2(900f, 740f));
        hud.victoryStatsText = AddText(winWindow, "Resumo", "", titleFont, 48, UiText, TextAnchor.MiddleCenter,
                                       center, new Vector2(0f, 130f), new Vector2(560f, 200f));
        hud.victoryStatsText.lineSpacing = 1f;
        hud.victoryRecordText = AddText(winWindow, "Recorde", "", titleFont, 52, UiGold, TextAnchor.MiddleCenter,
                                        center, new Vector2(0f, -40f), new Vector2(820f, 50f));
        hud.victoryRestartButton = AddButton(winWindow, "BotaoJogar", "JOGAR DE NOVO", new Vector2(0f, -150f), new Vector2(520f, 76f));
        hud.victoryMenuButton = AddButton(winWindow, "BotaoMenu", "MENU PRINCIPAL", new Vector2(0f, -246f), new Vector2(520f, 76f));
        winRoot.AddComponent<MenuFocus>().firstSelected = hud.victoryRestartButton.gameObject;
        hud.victoryPanel = winRoot;

        return hud;
    }

    // ------------------------------------------------------------------ menu

    static void BuildMenuScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Vector3(0f, 0f, -10f), 5f);

        var managers = new GameObject("AudioManager");
        AddAudioManager(managers, Music("music_menu"), 0.5f);

        Canvas canvas = CreateCanvas("Menu", 0);
        Transform root = canvas.transform;
        Vector2 center = new Vector2(0.5f, 0.5f);

        RectTransform bg = Stretch(NewUI("Fundo", root));
        var raw = bg.gameObject.AddComponent<RawImage>();
        raw.texture = Load<Texture2D>(ArtDir + "Backgrounds/space.png");
        raw.raycastTarget = false;
        bg.gameObject.AddComponent<UVScroller>();

        AddDecoration(root, Spr("player"), new Vector2(-640f, 60f), 220f, 20f, 0f, 0f);
        AddDecoration(root, Spr("crystal"), new Vector2(800f, 400f), 120f, 14f, 20f, 1f);
        AddDecoration(root, Spr("crystal"), new Vector2(760f, -40f), 80f, 10f, -30f, 2f);
        AddDecoration(root, Spr("crystal"), new Vector2(-780f, -320f), 90f, 12f, 25f, 3f);
        AddDecoration(root, Spr("alien"), new Vector2(640f, -300f), 170f, 16f, 0f, 4f);
        AddDecoration(root, Spr("drone"), new Vector2(-820f, 410f), 120f, 10f, 80f, 5f);
        AddDecoration(root, Spr("oxygen_tank"), new Vector2(-560f, -280f), 90f, 14f, -15f, 6f);

        AddText(root, "Titulo", "COLETOR ESTELAR", pixelFont, 88, UiGold, TextAnchor.MiddleCenter,
                center, new Vector2(0f, 340f), new Vector2(1800f, 120f)).GetComponent<Shadow>().effectDistance = new Vector2(8f, -8f);
        AddText(root, "Subtitulo", "Colete os cristais e fuja da estação antes que o oxigênio acabe!", bodyFont, 48, UiCyan,
                TextAnchor.MiddleCenter, center, new Vector2(0f, 250f), new Vector2(1600f, 60f));

        Text best = AddText(root, "Recorde", "", titleFont, 44, UiMuted, TextAnchor.MiddleCenter,
                            new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1600f, 40f));
        AddText(root, "Rodape", "Projeto Intro Unity · Yuri Henrique", bodyFont, 32, new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(-24f, 16f), new Vector2(800f, 40f), new Vector2(1f, 0f));

        // ---------- painel principal
        RectTransform mainPanel = Stretch(NewUI("PainelPrincipal", root));
        Vector2 buttonSize = new Vector2(560f, 84f);
        Button play = AddButton(mainPanel, "BotaoJogar", "JOGAR", new Vector2(0f, 80f), buttonSize, 60);
        Button howTo = AddButton(mainPanel, "BotaoComoJogar", "COMO JOGAR", new Vector2(0f, -24f), buttonSize, 52);
        Button credits = AddButton(mainPanel, "BotaoCreditos", "CRÉDITOS", new Vector2(0f, -128f), buttonSize, 52);
        Button quit = AddButton(mainPanel, "BotaoSair", "SAIR", new Vector2(0f, -232f), buttonSize, 52);
        mainPanel.gameObject.AddComponent<MenuFocus>().firstSelected = play.gameObject;

        // ---------- como jogar
        var (howRoot, howWindow) = AddPanel(root, "PainelComoJogar", "COMO JOGAR", UiGold, new Vector2(1500f, 900f));
        AddText(howWindow, "Texto",
            "<color=#5ad1ff>OBJETIVO</color>\n" +
            "Você é um astronauta preso numa estação espacial. Colete os 12 cristais de energia para\n" +
            "liberar a cápsula de fuga (canto superior direito) e entre nela antes que o oxigênio acabe!\n\n" +
            "<color=#5ad1ff>CONTROLES</color>          <color=#ffd23f>TECLADO</color>              <color=#ffd23f>CONTROLE XBOX</color>\n" +
            "Mover              WASD / Setas         Analógico esquerdo / D-pad\n" +
            "Dash               Espaço / Shift       A / RT\n" +
            "Pausar             Esc / P              Start\n" +
            "Menus              Setas + Enter        D-pad + A  (B volta)\n\n" +
            "<color=#5ad1ff>ITENS E PERIGOS</color>\n" +
            "Cristal: +100 pontos e +3s de oxigênio     Tanque de O2: +10s de oxigênio\n" +
            "Drones patrulham rotas fixas · Alienígenas perseguem você quando te enxergam\n" +
            "Minas ficam paradas · Lasers ligam e desligam: observe o ritmo e passe na hora certa!\n" +
            "O dash gasta stamina e te deixa invulnerável por um instante. Você tem 3 vidas.",
            bodyFont, 38, UiText, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1380f, 640f), new Vector2(0.5f, 1f));
        Button howBack = AddButton(howWindow, "BotaoVoltar", "VOLTAR", new Vector2(0f, -370f), new Vector2(420f, 76f));
        howRoot.AddComponent<MenuFocus>().firstSelected = howBack.gameObject;

        // ---------- créditos
        var (creditsRoot, creditsWindow) = AddPanel(root, "PainelCreditos", "CRÉDITOS", UiGold, new Vector2(1400f, 820f));
        AddText(creditsWindow, "Texto",
            "<color=#ffd23f>COLETOR ESTELAR</color>\n" +
            "Projeto Individual · Introdução ao Unity\n\n" +
            "Desenvolvimento: Yuri Henrique\n" +
            "Base: tutorial \"Introdução Rápida\" da disciplina de Jogos Digitais\n\n" +
            "Sprites (pixel art), músicas e efeitos sonoros gerados por código\n" +
            "pelo autor (Tools/generate_assets.py)\n\n" +
            "Fontes: Press Start 2P (CodeMan38) e VT323 (Peter Hull)\n" +
            "SIL Open Font License 1.1\n\n" +
            "Feito com Unity 6.3 LTS · URP 2D · Input System",
            bodyFont, 40, UiText, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1300f, 560f), new Vector2(0.5f, 1f));
        Button creditsBack = AddButton(creditsWindow, "BotaoVoltar", "VOLTAR", new Vector2(0f, -330f), new Vector2(420f, 76f));
        creditsRoot.AddComponent<MenuFocus>().firstSelected = creditsBack.gameObject;

        var menu = canvas.gameObject.AddComponent<MainMenu>();
        menu.mainPanel = mainPanel.gameObject;
        menu.howToPanel = howRoot;
        menu.creditsPanel = creditsRoot;
        menu.playButton = play;
        menu.howToButton = howTo;
        menu.creditsButton = credits;
        menu.quitButton = quit;
        menu.howToBackButton = howBack;
        menu.creditsBackButton = creditsBack;
        menu.bestText = best;
        howRoot.SetActive(false);
        creditsRoot.SetActive(false);

        CreateEventSystem();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MenuScenePath);
        log.AppendLine("Cena do menu salva.");
    }

    static void AddDecoration(Transform parent, Sprite sprite, Vector2 position, float size, float bob, float rotate, float phase)
    {
        Image image = AddImage(parent, "Decoracao", sprite, Color.white, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        image.preserveAspect = true;
        var floater = image.gameObject.AddComponent<Floater>();
        floater.bobAmplitude = bob;
        floater.rotateSpeed = rotate;
        floater.phase = phase;
    }

    // ------------------------------------------------------------------ projeto

    static void ConfigureProject()
    {
        PlayerSettings.productName = "Coletor Estelar";
        PlayerSettings.companyName = "Yuri Henrique";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        log.AppendLine("Configurações do projeto e Build Settings atualizados.");
    }

    // ------------------------------------------------------------------ build

    [MenuItem("Coletor Estelar/Gerar Build WebGL")]
    public static void BuildWebGL()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { MenuScenePath, GameScenePath },
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"Build WebGL: {report.summary.result} | {report.summary.totalSize / (1024 * 1024f):0.0} MB | {report.summary.totalTime}");
    }
}
