using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
    static PhysicsMaterial2D noFriction;

    static GameObject playerPrefab, crystalPrefab, oxygenPrefab, dronePrefab, alienPrefab, minePrefab, podPrefab;

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

        // por enquanto a fonte padrão do Unity
        pixelFont = bodyFont = titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateMaterials();

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
    // sprites padrão da interface do Unity, até a interface ganhar arte própria
    static Sprite DefaultUi(string file) => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/" + file);

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
        pc.visual = visual.transform;
        pc.body = visual.GetComponent<SpriteRenderer>();
        pc.audioSource = audio;
        pc.collectClip = Sfx("collect");
        playerPrefab = SavePrefab(player);

        // ---------- Cristal (o triângulo coletável do tutorial)
        var crystal = new GameObject("Cristal") { tag = "Coletavel" };
        var poly = crystal.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.pathCount = 1;
        poly.SetPath(0, new[] { new Vector2(0f, 0.41f), new Vector2(-0.38f, -0.38f), new Vector2(0.38f, -0.38f) });
        GameObject crystalVisual = Shape("Visual", crystal.transform, "triangle", new Color(0.35f, 0.9f, 1f), 5);
        var cc = crystal.AddComponent<Collectible>();
        cc.kind = Collectible.Kind.Crystal;
        cc.points = 100;
        cc.oxygen = 3f;
        cc.visual = crystalVisual.transform;
        crystalPrefab = SavePrefab(crystal);

        // ---------- Tanque de oxigênio
        var tank = new GameObject("TanqueOxigenio") { tag = "Coletavel" };
        var box = tank.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.5f, 0.8f);
        GameObject tankVisual = Shape("Visual", tank.transform, "circle", new Color(0.3f, 0.95f, 0.45f), 5);
        var tc = tank.AddComponent<Collectible>();
        tc.kind = Collectible.Kind.Oxygen;
        tc.points = 50;
        tc.oxygen = 10f;
        tc.visual = tankVisual.transform;
        oxygenPrefab = SavePrefab(tank);

        // ---------- Drone de patrulha
        var drone = new GameObject("Drone") { tag = "Inimigo" };
        var drb = drone.AddComponent<Rigidbody2D>();
        drb.bodyType = RigidbodyType2D.Kinematic;
        drb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var dcol = drone.AddComponent<CircleCollider2D>();
        dcol.isTrigger = true;
        dcol.radius = 0.4f;
        Shape("Visual", drone.transform, "circle", new Color(1f, 0.45f, 0.2f), 10, 0.8f);
        drone.AddComponent<PatrolEnemy>();
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
        GameObject alienVisual = Shape("Visual", alien.transform, "circle", new Color(0.4f, 0.9f, 0.35f), 11, 0.85f);
        var chaser = alien.AddComponent<ChaserEnemy>();
        chaser.bodyCollider = bodyCollider;
        chaser.body = alienVisual.GetComponent<SpriteRenderer>();
        alienPrefab = SavePrefab(alien);

        // ---------- Mina (inimigo parado)
        var mine = new GameObject("Mina") { tag = "Inimigo" };
        var mcol = mine.AddComponent<CircleCollider2D>();
        mcol.isTrigger = true;
        mcol.radius = 0.36f;
        Shape("Visual", mine.transform, "circle", new Color(0.75f, 0.2f, 0.55f), 8, 0.75f);
        minePrefab = SavePrefab(mine);

        // ---------- Cápsula de fuga
        var pod = new GameObject("CapsulaDeFuga") { tag = "Saida" };
        var pcol = pod.AddComponent<CircleCollider2D>();
        pcol.isTrigger = true;
        pcol.radius = 0.5f;
        GameObject podVisual = Shape("Visual", pod.transform, "circle", Color.white, 4, 1.4f);
        var exit = pod.AddComponent<ExitPod>();
        exit.body = podVisual.GetComponent<SpriteRenderer>();
        podPrefab = SavePrefab(pod);

        log.AppendLine("Prefabs criados.");
    }

    // ------------------------------------------------------------------ cena do jogo

    static void BuildGameScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam = CreateCamera(new Vector3(0f, 0.5f, -10f), 10.1f);
        cam.gameObject.AddComponent<CameraController>();

        // ---------- Arena
        Transform arena = new GameObject("Arena").transform;
        GameObject floor = Shape("Piso", arena, "square", new Color(0.11f, 0.13f, 0.2f), -10);
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
        GameObject wall = Shape("Parede", parent, "square", new Color(0.27f, 0.33f, 0.48f), 0);
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

        GameObject e1 = Shape("EmissorA", root.transform, "square", new Color(0.45f, 0.5f, 0.6f), 8, 0.5f);
        e1.transform.position = a;
        e1.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        GameObject e2 = Shape("EmissorB", root.transform, "square", new Color(0.45f, 0.5f, 0.6f), 8, 0.5f);
        e2.transform.position = b;
        e2.transform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);

        var gate = root.AddComponent<LaserGate>();
        gate.beam = beamRenderer;
        gate.beamCollider = beamCollider;
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
        return cam;
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
        Image image = AddImage(parent, name, DefaultUi("UISprite.psd"), Color.white, new Vector2(0.5f, 0.5f), position, size, true);
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
        return button;
    }

    static (GameObject root, RectTransform window) AddPanel(Transform parent, string name, string title, Color titleColor, Vector2 size)
    {
        RectTransform root = Stretch(NewUI(name, parent));
        var dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0.02f, 0.72f);
        dim.raycastTarget = true;

        Image window = AddImage(root, "Janela", DefaultUi("Background.psd"), new Color(0.09f, 0.12f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, size, true);
        AddText(window.transform, "Titulo", title, titleFont, 80, titleColor, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(size.x - 40f, 80f));
        return (root.gameObject, window.rectTransform);
    }

    static (RectTransform fill, Image fillImage) AddBar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        Image frame = AddImage(parent, name, Spr("square"), new Color(0.05f, 0.06f, 0.12f), anchor, position, size, false, new Vector2(1f, 0.5f));
        RectTransform area = Stretch(NewUI("Area", frame.transform), 8f);
        RectTransform fill = NewUI("Preenchimento", area);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.sprite = Spr("square");
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
        Image bar = AddImage(root, "BarraSuperior", Spr("square"), new Color(0.03f, 0.04f, 0.09f, 0.85f),
                             new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 112f), false, new Vector2(0.5f, 1f));
        bar.rectTransform.anchorMin = new Vector2(0f, 1f);
        bar.rectTransform.anchorMax = new Vector2(1f, 1f);
        Transform b = bar.transform;

        AddText(b, "RotuloCristais", "CRISTAIS", pixelFont, 16, UiMuted, TextAnchor.MiddleLeft,
                left, new Vector2(40f, 24f), new Vector2(240f, 30f), new Vector2(0f, 0.5f));
        hud.crystalText = AddText(b, "Cristais", "0/12", pixelFont, 32, UiCyan, TextAnchor.MiddleLeft,
                                  left, new Vector2(40f, -16f), new Vector2(240f, 50f), new Vector2(0f, 0.5f));
        AddText(b, "RotuloPontos", "PONTOS", pixelFont, 16, UiMuted, TextAnchor.MiddleLeft,
                left, new Vector2(310f, 24f), new Vector2(240f, 30f), new Vector2(0f, 0.5f));
        hud.scoreText = AddText(b, "Pontos", "00000", pixelFont, 32, UiGold, TextAnchor.MiddleLeft,
                                left, new Vector2(310f, -16f), new Vector2(240f, 50f), new Vector2(0f, 0.5f));

        AddText(b, "RotuloTempo", "TEMPO", pixelFont, 16, UiMuted, TextAnchor.MiddleCenter,
                center, new Vector2(0f, 26f), new Vector2(300f, 30f));
        hud.timeText = AddText(b, "Tempo", "00:00.00", pixelFont, 40, UiText, TextAnchor.MiddleCenter,
                               center, new Vector2(16f, -14f), new Vector2(380f, 56f));

        hud.hearts = new Image[3];
        for (int i = 0; i < 3; i++)
            hud.hearts[i] = AddImage(b, "Coracao" + i, Spr("circle"), Color.white, right,
                                     new Vector2(-188f + i * 64f, 0f), new Vector2(44f, 44f));

        AddText(b, "RotuloOxigenio", "O2", pixelFont, 16, UiMuted, TextAnchor.MiddleRight,
                right, new Vector2(-626f, 20f), new Vector2(80f, 34f));
        (hud.oxygenFill, hud.oxygenFillImage) = AddBar(b, "BarraOxigenio", right, new Vector2(-250f, 20f), new Vector2(330f, 34f), UiCyan);
        hud.oxygenText = AddText(hud.oxygenFill.parent.parent, "TextoOxigenio", "60s", pixelFont, 16, UiText, TextAnchor.MiddleCenter,
                                 center, new Vector2(0f, 0f), new Vector2(320f, 30f));

        // ---------- mensagens
        hud.messageText = AddText(root, "Mensagem", "", titleFont, 60, new Color(1f, 0.9f, 0.45f), TextAnchor.MiddleCenter,
                                  new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1600f, 160f));
        hud.messageText.lineSpacing = 0.9f;
        var outline = hud.messageText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        // ---------- pausa
        var (pauseRoot, pauseWindow) = AddPanel(root, "PainelPausa", "PAUSADO", Color.white, new Vector2(900f, 680f));
        AddText(pauseWindow, "Dicas",
                "<color=#ffd23f>MOVER</color>  WASD / SETAS\n<color=#ffd23f>PAUSA</color>  ESC / P",
                bodyFont, 40, UiText, TextAnchor.MiddleCenter, center, new Vector2(0f, 130f), new Vector2(820f, 160f));
        hud.resumeButton = AddButton(pauseWindow, "BotaoContinuar", "CONTINUAR", new Vector2(0f, -40f), new Vector2(520f, 76f));
        hud.pauseRestartButton = AddButton(pauseWindow, "BotaoReiniciar", "REINICIAR", new Vector2(0f, -136f), new Vector2(520f, 76f));
        hud.pauseMenuButton = AddButton(pauseWindow, "BotaoMenu", "MENU PRINCIPAL", new Vector2(0f, -232f), new Vector2(520f, 76f));
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
        hud.victoryPanel = winRoot;

        return hud;
    }

    // ------------------------------------------------------------------ menu

    static void BuildMenuScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Vector3(0f, 0f, -10f), 5f);

        Canvas canvas = CreateCanvas("Menu", 0);
        Transform root = canvas.transform;
        Vector2 center = new Vector2(0.5f, 0.5f);

        AddText(root, "Titulo", "COLETOR ESTELAR", pixelFont, 88, UiGold, TextAnchor.MiddleCenter,
                center, new Vector2(0f, 340f), new Vector2(1800f, 120f)).GetComponent<Shadow>().effectDistance = new Vector2(8f, -8f);
        AddText(root, "Subtitulo", "Colete os cristais e fuja da estação antes que o oxigênio acabe!", bodyFont, 48, UiCyan,
                TextAnchor.MiddleCenter, center, new Vector2(0f, 250f), new Vector2(1600f, 60f));

        // ---------- painel principal
        RectTransform mainPanel = Stretch(NewUI("PainelPrincipal", root));
        Vector2 buttonSize = new Vector2(560f, 84f);
        Button play = AddButton(mainPanel, "BotaoJogar", "JOGAR", new Vector2(0f, 80f), buttonSize, 60);
        Button quit = AddButton(mainPanel, "BotaoSair", "SAIR", new Vector2(0f, -24f), buttonSize, 52);

        var menu = canvas.gameObject.AddComponent<MainMenu>();
        menu.playButton = play;
        menu.quitButton = quit;

        CreateEventSystem();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MenuScenePath);
        log.AppendLine("Cena do menu salva.");
    }

    // ------------------------------------------------------------------ projeto

    static void ConfigureProject()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        log.AppendLine("Build Settings atualizados.");
    }
}
