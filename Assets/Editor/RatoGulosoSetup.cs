using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Monta o jogo inteiro dentro da Unity com um clique:
//   menu "Rato Guloso > 1. Montar projeto"  -> configura sprites, cria prefabs, as 3 cenas e o Build Settings
//   menu "Rato Guloso > 2. Gerar build WebGL" -> gera a pasta Builds/WebGL para enviar ao itch.io
// Depois de montado, tudo pode ser editado normalmente pelo Editor (cenas em Assets/Scenes, prefabs em Assets/Prefabs).
public static class RatoGulosoSetup
{
    // >>> Troque pelo seu nome completo antes de rodar (aparece no rodapé do menu e em Company Name)
    const string AuthorName = "Enzo Canavero";

    const string ArtDir = "Assets/Art";
    const string FontDir = "Assets/Fonts";
    const string SceneDir = "Assets/Scenes";
    const string PrefabDir = "Assets/Prefabs";
    const string MenuScenePath = SceneDir + "/MainMenu.unity";
    const string GameScenePath = SceneDir + "/Game.unity";
    const string EndScenePath = SceneDir + "/GameOver.unity";

    // ------------------------------------------------------------------ MAPA DA FASE
    // Cada caractere = 0,5 unidade (38 x 20). Edite e rode o menu de novo para mudar a cozinha!
    //   .  chão                   #  balcão (parede)
    //   P  rato (jogador)         Q  queijo            K  relógio (+10s)       T  ratoeira
    //   X  gato preto (persegue)  x  pontos de patrulha do gato preto
    //   a..f  gato laranja: início da patrulha   A..F  fim da patrulha do gato com a mesma letra
    // MAP_BEGIN
    static readonly string[] Map =
    {
        "......................................",
        ".Q..........Q..............Q........Q.",
        "......................................",
        "...#######.....##########.....#####...",
        "...#######.....##########.....#####...",
        "...##............................##...",
        "...##...a..........Q....x...AK...##...",
        "...##..Q.........................##...",
        "..........######......######..........",
        "..........######......######...T....X.",
        ".Q........##..............##..........",
        ".......K..##..b...Q....B..##..x.......",
        "..........##.......x......##........Q.",
        "...######....................######...",
        "...######.......#######......######...",
        "................#######...............",
        ".....T.......T.............T..........",
        "........Q................K............",
        ".P..........c......Q.............C..Q.",
        "......................................",
    };
    // MAP_END

    const float Cell = 0.5f;
    static readonly Vector2 ArenaMin = new Vector2(-9.5f, -5.5f);
    static readonly Vector2 ArenaMax = new Vector2(9.5f, 4.5f);

    // Cores (os sprites básicos são tingidos pelo SpriteRenderer.color / Image.color)
    static readonly Color OrangeCat = new Color(1f, 0.6f, 0.22f);
    static readonly Color FloorTint = new Color(1f, 0.97f, 0.92f);
    static readonly Color CounterTint = new Color(1f, 0.95f, 0.9f);
    static readonly Color Brown = new Color(0.3f, 0.15f, 0.04f);
    static readonly Color Cheese = new Color(1f, 0.8f, 0.2f);
    static readonly Color DarkBg = new Color(0.17f, 0.1f, 0.06f);

    class Res
    {
        public Sprite rato, queijo, relogio, gato, gatoPreto, ratoeira, ratoeiraFechada;
        public Sprite piso, balcao, parede, coracao, raio, alerta, painel;
        public Material spriteMat;
        public PhysicsMaterial2D noFriction;
        public Font fontBlack, fontBold;
        public GameObject cheesePrefab, clockPrefab, trapPrefab, catPrefab, blackCatPrefab;
    }

    // ================================================================== MENUS
    [MenuItem("Rato Guloso/1. Montar projeto (cenas, prefabs e build)", false, 1)]
    public static void SetupAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            EditorUtility.DisplayProgressBar("Rato Guloso", "Configurando sprites...", 0.1f);
            ConfigureSpriteImports();
            EnsureFolder(SceneDir);
            EnsureFolder(PrefabDir);
            Res r = LoadResources();

            EditorUtility.DisplayProgressBar("Rato Guloso", "Criando prefabs...", 0.3f);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildPrefabs(r);

            EditorUtility.DisplayProgressBar("Rato Guloso", "Criando cenas...", 0.5f);
            BuildMenuScene(r);
            BuildGameScene(r);
            BuildEndScene(r);

            EditorUtility.DisplayProgressBar("Rato Guloso", "Configurando build...", 0.9f);
            ConfigureBuildSettings(true);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        EditorSceneManager.OpenScene(MenuScenePath);
        EditorUtility.DisplayDialog("Rato Guloso",
            "Projeto montado!\n\nCenas: Assets/Scenes (MainMenu, Game, GameOver)\nPrefabs: Assets/Prefabs\n\n" +
            "Aperte Play com a cena MainMenu aberta para testar.", "OK");
    }

    [MenuItem("Rato Guloso/2. Gerar build WebGL (pasta Builds/WebGL)", false, 2)]
    public static void BuildWebGL()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            EditorUtility.DisplayDialog("Rato Guloso",
                "O módulo 'WebGL Build Support' não está instalado.\n\nUnity Hub > Installs > engrenagem da sua versão > Add modules > WebGL Build Support.", "OK");
            return;
        }
        if (!File.Exists(MenuScenePath)) SetupAll();
        ConfigureBuildSettings(false);

        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "WebGL"));
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { MenuScenePath, GameScenePath, EndScenePath },
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.RevealInFinder(Path.Combine(outDir, "index.html"));
            EditorUtility.DisplayDialog("Rato Guloso",
                "Build WebGL pronta em:\n" + outDir + "\n\nCompacte o CONTEÚDO dessa pasta em um .zip (o index.html precisa ficar na raiz do zip) e envie ao itch.io.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Rato Guloso", "A build falhou: " + report.summary.result + ". Veja o Console.", "OK");
        }
    }

    // ================================================================== ASSETS
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void ConfigureSpriteImports()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            string file = Path.GetFileNameWithoutExtension(path);
            bool isTileOrPanel = file == "piso" || file == "balcao" || file == "parede" || file == "ui_painel";
            ti.mipmapEnabled = !isTileOrPanel; // evita "cintilar" quando o sprite aparece bem menor que 128 px
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterSettings settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;   // necessário para Draw Mode "Tiled"/"Sliced"
            settings.spritePixelsPerUnit = 128f;                 // 128 px = 1 unidade
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = file == "ui_painel"
                ? new Vector4(20f, 20f, 20f, 20f)
                : Vector4.zero;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
    }

    static Sprite LoadSprite(string name)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + name + ".png");
        if (s == null) throw new System.Exception("Sprite não encontrado: " + ArtDir + "/" + name + ".png");
        return s;
    }

    static Res LoadResources()
    {
        Res r = new Res
        {
            rato = LoadSprite("rato"),
            queijo = LoadSprite("queijo"),
            relogio = LoadSprite("relogio"),
            gato = LoadSprite("gato"),
            gatoPreto = LoadSprite("gato_preto"),
            ratoeira = LoadSprite("ratoeira"),
            ratoeiraFechada = LoadSprite("ratoeira_fechada"),
            piso = LoadSprite("piso"),
            balcao = LoadSprite("balcao"),
            parede = LoadSprite("parede"),
            coracao = LoadSprite("coracao"),
            raio = LoadSprite("raio"),
            alerta = LoadSprite("alerta"),
            painel = LoadSprite("ui_painel"),
        };

        // Material sem iluminação: os sprites aparecem com a cor certa mesmo sem luzes 2D (URP) na cena.
        Shader shader = null;
        if (GraphicsSettings.currentRenderPipeline != null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        string matPath = ArtDir + "/SpriteUnlit.mat";
        r.spriteMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (r.spriteMat == null)
        {
            r.spriteMat = new Material(shader);
            AssetDatabase.CreateAsset(r.spriteMat, matPath);
        }
        else
        {
            r.spriteMat.shader = shader;
            EditorUtility.SetDirty(r.spriteMat);
        }

        // Sem atrito: o rato e o gato preto deslizam nas paredes em vez de "grudar"
        string pmPath = ArtDir + "/SemAtrito.physicsMaterial2D";
        r.noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(pmPath);
        if (r.noFriction == null)
        {
            r.noFriction = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(r.noFriction, pmPath);
        }

        r.fontBlack = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/Inter-Black.otf");
        r.fontBold = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/Inter-Bold.otf");
#if UNITY_2022_2_OR_NEWER
        Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        Font builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        if (r.fontBlack == null) r.fontBlack = builtin;
        if (r.fontBold == null) r.fontBold = builtin;
        return r;
    }

    // ================================================================== PREFABS
    static GameObject Child(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static SpriteRenderer AddSprite(GameObject go, Sprite sprite, int order, Res r)
    {
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = r.spriteMat;
        sr.sortingOrder = order;
        return sr;
    }

    static GameObject SavePrefab(GameObject go)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + go.name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void BuildPrefabs(Res r)
    {
        // Queijo: +10 pontos e +3 segundos
        GameObject cheese = new GameObject("Queijo");
        CircleCollider2D cc = cheese.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.3f;
        GameObject cheeseVisual = Child(cheese, "Visual");
        cheeseVisual.transform.localScale = Vector3.one * 0.6f;
        AddSprite(cheeseVisual, r.queijo, 2, r);
        Collectible cheeseItem = cheese.AddComponent<Collectible>();
        cheeseItem.kind = Collectible.Kind.Cheese;
        cheeseItem.points = 10;
        cheeseItem.timeBonus = 3f;
        cheeseItem.visual = cheeseVisual.transform;
        r.cheesePrefab = SavePrefab(cheese);

        // Relógio: +10 segundos
        GameObject clock = new GameObject("Relogio");
        CircleCollider2D kc = clock.AddComponent<CircleCollider2D>();
        kc.isTrigger = true;
        kc.radius = 0.3f;
        GameObject clockVisual = Child(clock, "Visual");
        clockVisual.transform.localScale = Vector3.one * 0.55f;
        AddSprite(clockVisual, r.relogio, 2, r);
        Collectible clockItem = clock.AddComponent<Collectible>();
        clockItem.kind = Collectible.Kind.Clock;
        clockItem.points = 5;
        clockItem.timeBonus = 10f;
        clockItem.visual = clockVisual.transform;
        clockItem.bobSpeed = 4f;
        r.clockPrefab = SavePrefab(clock);

        // Ratoeira: inimigo parado. Fecha ao pegar o rato e fica desarmada por 4 s (não trava o caminho)
        GameObject trap = new GameObject("Ratoeira");
        BoxCollider2D tc = trap.AddComponent<BoxCollider2D>();
        tc.isTrigger = true;
        tc.size = new Vector2(0.62f, 0.36f);
        GameObject trapVisual = Child(trap, "Visual");
        trapVisual.transform.localScale = Vector3.one * 0.8f;
        SpriteRenderer trapSr = AddSprite(trapVisual, r.ratoeira, 1, r);
        Hazard trapHazard = trap.AddComponent<Hazard>();
        trapHazard.rearmTime = 4f;
        trapHazard.spriteRenderer = trapSr;
        trapHazard.armedSprite = r.ratoeira;
        trapHazard.snappedSprite = r.ratoeiraFechada;
        r.trapPrefab = SavePrefab(trap);

        // Gato laranja: patrulha (Kinematic + trigger)
        GameObject cat = new GameObject("GatoLaranja");
        Rigidbody2D catRb = cat.AddComponent<Rigidbody2D>();
        catRb.bodyType = RigidbodyType2D.Kinematic;
        catRb.gravityScale = 0f;
        catRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        CircleCollider2D catCol = cat.AddComponent<CircleCollider2D>();
        catCol.isTrigger = true;
        catCol.radius = 0.36f;
        cat.AddComponent<Hazard>();
        GameObject catVisual = Child(cat, "Visual");
        catVisual.transform.localScale = Vector3.one * 0.95f;
        SpriteRenderer catSr = AddSprite(catVisual, r.gato, 4, r);
        catSr.color = OrangeCat; // sprite branco tingido de laranja
        EnemyPatrol patrol = cat.AddComponent<EnemyPatrol>();
        patrol.visual = catVisual.transform;
        r.catPrefab = SavePrefab(cat);

        // Gato preto: persegue (Dynamic). Collider sólido bate nas paredes; o trigger causa o dano.
        GameObject black = new GameObject("GatoPreto");
        Rigidbody2D blackRb = black.AddComponent<Rigidbody2D>();
        blackRb.gravityScale = 0f;
        blackRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        blackRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        blackRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D solid = black.AddComponent<CircleCollider2D>();
        solid.radius = 0.3f;
        solid.sharedMaterial = r.noFriction;
        CircleCollider2D hurt = black.AddComponent<CircleCollider2D>();
        hurt.isTrigger = true;
        hurt.radius = 0.4f;
        black.AddComponent<Hazard>();
        GameObject blackVisual = Child(black, "Visual");
        AddSprite(blackVisual, r.gatoPreto, 4, r);
        GameObject alert = Child(black, "Alerta");
        alert.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        alert.transform.localScale = Vector3.one * 0.8f;
        SpriteRenderer alertSr = AddSprite(alert, r.alerta, 6, r);
        alertSr.enabled = false;
        EnemyChaser chaser = black.AddComponent<EnemyChaser>();
        chaser.solidCollider = solid;
        chaser.visual = blackVisual.transform;
        chaser.alertIcon = alertSr;
        r.blackCatPrefab = SavePrefab(black);
    }

    static GameObject Place(GameObject prefab, Vector2 position, Transform parent)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        return go;
    }

    // ================================================================== CENA DO JOGO
    static Vector2 CellToWorld(int col, int row)
    {
        return new Vector2(ArenaMin.x + (col + 0.5f) * Cell, ArenaMax.y - (row + 0.5f) * Cell);
    }

    // Junta os '#' vizinhos em retângulos (menos objetos e colliders)
    static List<Rect> MergeWalls()
    {
        int h = Map.Length, w = Map[0].Length;
        bool[,] used = new bool[w, h];
        List<Rect> rects = new List<Rect>();
        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                if (Map[row][col] != '#' || used[col, row]) continue;
                int rw = 1;
                while (col + rw < w && Map[row][col + rw] == '#' && !used[col + rw, row]) rw++;
                int rh = 1;
                while (row + rh < h && RowSpanFree(row + rh, col, rw, used)) rh++;
                for (int y = row; y < row + rh; y++)
                    for (int x = col; x < col + rw; x++) used[x, y] = true;
                float xMin = ArenaMin.x + col * Cell;
                float yMax = ArenaMax.y - row * Cell;
                rects.Add(new Rect(xMin, yMax - rh * Cell, rw * Cell, rh * Cell));
            }
        }
        return rects;
    }

    static bool RowSpanFree(int row, int col, int width, bool[,] used)
    {
        for (int k = 0; k < width; k++)
            if (Map[row][col + k] != '#' || used[col + k, row]) return false;
        return true;
    }

    static void CreateWall(GameObject parent, string name, Rect rect, Sprite sprite, Color color, Res r, bool shadow)
    {
        GameObject wall = Child(parent, name);
        wall.transform.position = rect.center;
        SpriteRenderer sr = AddSprite(wall, sprite, 0, r);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = rect.size;
        sr.color = color;
        BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
        box.size = rect.size;

        if (!shadow) return;
        GameObject sh = Child(wall, "Sombra");
        sh.transform.localPosition = new Vector3(0.08f, -0.12f, 0f);
        SpriteRenderer ssr = AddSprite(sh, r.painel, -5, r);
        ssr.drawMode = SpriteDrawMode.Sliced;
        ssr.size = rect.size + new Vector2(0.06f, 0.06f);
        ssr.color = new Color(0f, 0f, 0f, 0.25f);
    }

    static void BuildGameScene(Res r)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera cam = CreateCamera(DarkBg, 6.25f, new Vector3(0f, 0.25f, -10f));
        cam.gameObject.AddComponent<CameraController>();

        // --- Cozinha (chão, paredes e balcões)
        GameObject level = new GameObject("Cozinha");
        GameObject floor = Child(level, "Chao");
        floor.transform.position = (ArenaMin + ArenaMax) / 2f;
        SpriteRenderer floorSr = AddSprite(floor, r.piso, -10, r);
        floorSr.drawMode = SpriteDrawMode.Tiled;
        floorSr.size = ArenaMax - ArenaMin;
        floorSr.color = FloorTint;

        GameObject walls = Child(level, "Paredes");
        CreateWall(walls, "ParedeCima", new Rect(-10f, 4.5f, 20f, 0.5f), r.parede, Color.white, r, false);
        CreateWall(walls, "ParedeBaixo", new Rect(-10f, -6f, 20f, 0.5f), r.parede, Color.white, r, false);
        CreateWall(walls, "ParedeEsquerda", new Rect(-10f, -5.5f, 0.5f, 10f), r.parede, Color.white, r, false);
        CreateWall(walls, "ParedeDireita", new Rect(9.5f, -5.5f, 0.5f, 10f), r.parede, Color.white, r, false);
        int n = 1;
        foreach (Rect rect in MergeWalls())
            CreateWall(walls, "Balcao" + n++, rect, r.balcao, CounterTint, r, true);

        // --- Objetos do mapa
        GameObject items = new GameObject("Itens");
        GameObject enemies = new GameObject("Inimigos");
        Vector2 playerPos = CellToWorld(1, Map.Length - 2);
        Vector2? chaserPos = null;
        List<Vector2> chaserPoints = new List<Vector2>();
        Dictionary<char, Vector2> patrolStart = new Dictionary<char, Vector2>();
        Dictionary<char, Vector2> patrolEnd = new Dictionary<char, Vector2>();

        for (int row = 0; row < Map.Length; row++)
        {
            for (int col = 0; col < Map[row].Length; col++)
            {
                char ch = Map[row][col];
                Vector2 p = CellToWorld(col, row);
                switch (ch)
                {
                    case 'P': playerPos = p; break;
                    case 'Q': Place(r.cheesePrefab, p, items.transform); break;
                    case 'K': Place(r.clockPrefab, p, items.transform); break;
                    case 'T': Place(r.trapPrefab, p, enemies.transform); break;
                    case 'X': chaserPos = p; break;
                    case 'x': chaserPoints.Add(p); break;
                    default:
                        if (ch >= 'a' && ch <= 'f') patrolStart[ch] = p;
                        else if (ch >= 'A' && ch <= 'F') patrolEnd[char.ToLowerInvariant(ch)] = p;
                        break;
                }
            }
        }

        foreach (KeyValuePair<char, Vector2> kv in patrolStart)
        {
            GameObject cat = Place(r.catPrefab, kv.Value, enemies.transform);
            cat.name = "GatoLaranja_" + kv.Key;
            PrefabUtility.RecordPrefabInstancePropertyModifications(cat);
            EnemyPatrol patrol = cat.GetComponent<EnemyPatrol>();
            patrol.points = patrolEnd.TryGetValue(kv.Key, out Vector2 end) ? new[] { kv.Value, end } : new[] { kv.Value };
            PrefabUtility.RecordPrefabInstancePropertyModifications(patrol);
        }

        if (chaserPos.HasValue)
        {
            GameObject black = Place(r.blackCatPrefab, chaserPos.Value, enemies.transform);
            EnemyChaser chaser = black.GetComponent<EnemyChaser>();
            List<Vector2> pts = new List<Vector2> { chaserPos.Value };
            pts.AddRange(chaserPoints);
            chaser.patrolPoints = pts.ToArray();
            PrefabUtility.RecordPrefabInstancePropertyModifications(chaser);
        }

        // --- Rato (jogador)
        GameObject spawn = new GameObject("SpawnDoRato");
        spawn.transform.position = playerPos;

        GameObject player = new GameObject("Rato");
        player.tag = "Player";
        player.transform.position = playerPos;
        Rigidbody2D prb = player.AddComponent<Rigidbody2D>();
        prb.gravityScale = 0f;
        prb.constraints = RigidbodyConstraints2D.FreezeRotation;
        prb.interpolation = RigidbodyInterpolation2D.Interpolate;
        prb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        prb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        CircleCollider2D pc = player.AddComponent<CircleCollider2D>();
        pc.radius = 0.3f;
        pc.sharedMaterial = r.noFriction;
        GameObject pv = Child(player, "Visual");
        pv.transform.localScale = Vector3.one * 0.9f;
        SpriteRenderer psr = AddSprite(pv, r.rato, 5, r);
        PlayerMovement pm = player.AddComponent<PlayerMovement>();
        pm.visual = pv.transform;
        pm.spriteRenderer = psr;

        // --- HUD
        Canvas canvas = CreateCanvas("HUD");
        CreateEventSystem();
        HUD hud = canvas.gameObject.AddComponent<HUD>();
        hud.player = pm;
        Transform ct = canvas.transform;

        Image bar = CreateImage(ct, "BarraSuperior", null, new Color(0.1f, 0.05f, 0.02f, 0.82f));
        SetRect(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 120f));

        // vidas (canto superior esquerdo)
        Vector2 topLeft = new Vector2(0f, 1f);
        hud.hearts = new Image[pm.maxLives];
        for (int i = 0; i < pm.maxLives; i++)
        {
            Image heart = CreateImage(ct, "Vida" + (i + 1), r.coracao, Color.white);
            heart.preserveAspect = true;
            SetRect(heart.rectTransform, topLeft, topLeft, new Vector2(0.5f, 0.5f), new Vector2(60f + i * 66f, -40f), new Vector2(58f, 58f));
            hud.hearts[i] = heart;
        }
        // stamina
        Image bolt = CreateImage(ct, "IconeStamina", r.raio, Color.white);
        bolt.preserveAspect = true;
        SetRect(bolt.rectTransform, topLeft, topLeft, new Vector2(0.5f, 0.5f), new Vector2(54f, -92f), new Vector2(40f, 40f));
        Image staminaBg = CreateImage(ct, "StaminaFundo", null, new Color(0f, 0f, 0f, 0.6f));
        SetRect(staminaBg.rectTransform, topLeft, topLeft, new Vector2(0f, 0.5f), new Vector2(82f, -92f), new Vector2(200f, 20f));
        Image staminaFill = CreateImage(staminaBg.transform, "StaminaBarra", null, new Color(1f, 0.82f, 0.2f));
        SetRect(staminaFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        hud.staminaFill = staminaFill.rectTransform;
        hud.staminaFillImage = staminaFill;

        // tempo (centro)
        Vector2 top = new Vector2(0.5f, 1f);
        Image clockIcon = CreateImage(ct, "IconeRelogio", r.relogio, Color.white);
        clockIcon.preserveAspect = true;
        SetRect(clockIcon.rectTransform, top, top, new Vector2(0.5f, 0.5f), new Vector2(-110f, -44f), new Vector2(64f, 64f));
        hud.timeLeftText = CreateText(ct, "TempoRestante", "1:00", r.fontBlack, 64, Color.white, TextAnchor.MiddleCenter);
        SetRect(hud.timeLeftText.rectTransform, top, top, new Vector2(0.5f, 0.5f), new Vector2(20f, -44f), new Vector2(200f, 80f));
        hud.stopwatchText = CreateText(ct, "Cronometro", "Cronômetro 00:00.0", r.fontBold, 26, new Color(1f, 0.92f, 0.75f), TextAnchor.MiddleCenter);
        SetRect(hud.stopwatchText.rectTransform, top, top, new Vector2(0.5f, 0.5f), new Vector2(0f, -98f), new Vector2(400f, 34f));
        hud.bonusText = CreateText(ct, "BonusTempo", "+3s", r.fontBlack, 42, new Color(0.45f, 1f, 0.45f), TextAnchor.MiddleCenter);
        SetRect(hud.bonusText.rectTransform, top, top, new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(300f, 60f));
        hud.bonusText.color = new Color(0.45f, 1f, 0.45f, 0f);

        // queijos e pontos (canto superior direito)
        Vector2 topRight = new Vector2(1f, 1f);
        Image cheeseIcon = CreateImage(ct, "IconeQueijo", r.queijo, Color.white);
        cheeseIcon.preserveAspect = true;
        SetRect(cheeseIcon.rectTransform, topRight, topRight, new Vector2(0.5f, 0.5f), new Vector2(-250f, -42f), new Vector2(64f, 64f));
        hud.cheeseText = CreateText(ct, "Queijos", "0/12", r.fontBlack, 48, Cheese, TextAnchor.MiddleLeft);
        SetRect(hud.cheeseText.rectTransform, topRight, topRight, new Vector2(0f, 0.5f), new Vector2(-205f, -42f), new Vector2(180f, 64f));
        hud.scoreText = CreateText(ct, "Pontos", "Pontos 0", r.fontBold, 30, Color.white, TextAnchor.MiddleRight);
        SetRect(hud.scoreText.rectTransform, topRight, topRight, new Vector2(1f, 0.5f), new Vector2(-30f, -94f), new Vector2(320f, 40f));

        // mensagens no meio da tela
        hud.messageText = CreateText(ct, "Mensagem", "", r.fontBlack, 72, Color.white, TextAnchor.MiddleCenter);
        SetRect(hud.messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1600f, 120f));
        hud.messageText.color = new Color(1f, 1f, 1f, 0f);

        // --- Painel de pausa
        Image pause = CreateImage(ct, "PainelPausa", null, new Color(0f, 0f, 0f, 0.75f));
        SetRect(pause.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Text pauseTitle = CreateText(pause.transform, "Titulo", "PAUSADO", r.fontBlack, 110, Cheese, TextAnchor.MiddleCenter);
        SetRect(pauseTitle.rectTransform, Center, Center, Center, new Vector2(0f, 230f), new Vector2(1200f, 150f));
        Button resume = CreateButton(pause.transform, "Continuar", new Vector2(0f, 70f), r);
        Button restart = CreateButton(pause.transform, "Reiniciar", new Vector2(0f, -60f), r);
        Button toMenu = CreateButton(pause.transform, "Menu Inicial", new Vector2(0f, -190f), r);
        Text pauseTip = CreateText(pause.transform, "Dica", "P / Esc / Start: continuar     M / Back: liga/desliga o som", r.fontBold, 28, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
        SetRect(pauseTip.rectTransform, Center, Center, Center, new Vector2(0f, -320f), new Vector2(1600f, 50f));
        pause.gameObject.AddComponent<UIFocus>().firstSelected = resume;
        pause.gameObject.SetActive(false);

        // --- GameManager
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.arenaMin = ArenaMin;
        gm.arenaMax = ArenaMax;
        gm.player = pm;
        gm.playerSpawn = spawn.transform;
        gm.hud = hud;
        gm.pausePanel = pause.gameObject;
        gm.resumeButton = resume;
        gm.restartButton = restart;
        gm.menuButton = toMenu;

        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    // ================================================================== MENU INICIAL
    static void BuildMenuScene(Res r)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(DarkBg, 5f, new Vector3(0f, 0f, -10f));
        Canvas canvas = CreateCanvas("Menu");
        CreateEventSystem();
        Transform ct = canvas.transform;
        CreateKitchenBackground(ct, r);

        // decoração
        Decoration(ct, "Rato", r.rato, Color.white, new Vector2(-600f, -40f), 380f, 20f);
        Decoration(ct, "Gato", r.gato, OrangeCat, new Vector2(620f, 40f), 360f, 200f);
        Decoration(ct, "GatoPreto", r.gatoPreto, Color.white, new Vector2(700f, -330f), 220f, 150f);
        Decoration(ct, "Queijo1", r.queijo, Color.white, new Vector2(-760f, 300f), 150f, 25f);
        Decoration(ct, "Queijo2", r.queijo, Color.white, new Vector2(-560f, -380f), 130f, -30f);
        Decoration(ct, "Queijo3", r.queijo, Color.white, new Vector2(430f, 330f), 120f, 160f);

        GameObject mainPanel = FullScreenPanel(ct, "PainelPrincipal");
        Text title = CreateText(mainPanel.transform, "Titulo", "RATO GULOSO", r.fontBlack, 150, Cheese, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, Center, Center, Center, new Vector2(0f, 330f), new Vector2(1600f, 200f));
        Outline titleOutline = title.GetComponent<Outline>();
        titleOutline.effectDistance = new Vector2(6f, -6f);
        Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        titleShadow.effectDistance = new Vector2(10f, -12f);
        Text subtitle = CreateText(mainPanel.transform, "Subtitulo", "A Corrida do Queijo", r.fontBold, 54, Color.white, TextAnchor.MiddleCenter);
        SetRect(subtitle.rectTransform, Center, Center, Center, new Vector2(0f, 215f), new Vector2(1200f, 80f));

        // botões do painel principal
        Button play = CreateButton(mainPanel.transform, "Jogar", new Vector2(0f, 50f), r);
        Button howTo = CreateButton(mainPanel.transform, "Como Jogar", new Vector2(0f, -80f), r);
        Button quit = CreateButton(mainPanel.transform, "Sair", new Vector2(0f, -210f), r);
        Text record = CreateText(mainPanel.transform, "Recorde", "Recorde: 0 pts", r.fontBold, 32, new Color(1f, 0.92f, 0.75f), TextAnchor.MiddleCenter);
        SetRect(record.rectTransform, Center, Center, Center, new Vector2(0f, -340f), new Vector2(1400f, 50f));
        mainPanel.AddComponent<UIFocus>().firstSelected = play;

        Text footer = CreateText(ct, "Creditos", "Feito por " + AuthorName + "  -  Jogos Digitais, Insper 2026", r.fontBold, 26, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter);
        SetRect(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1600f, 40f));

        // painel "Como jogar"
        GameObject howToPanel = FullScreenPanel(ct, "PainelComoJogar");
        Image box = CreateImage(howToPanel.transform, "Caixa", r.painel, new Color(0.12f, 0.06f, 0.02f, 0.94f));
        box.type = Image.Type.Sliced;
        SetRect(box.rectTransform, Center, Center, Center, Vector2.zero, new Vector2(1400f, 820f));
        Text howTitle = CreateText(box.transform, "Titulo", "COMO JOGAR", r.fontBlack, 64, Cheese, TextAnchor.MiddleCenter);
        SetRect(howTitle.rectTransform, top: true, pos: new Vector2(0f, -60f), size: new Vector2(1200f, 90f));
        string help =
            "Você é um rato guloso! Coma TODOS os queijos da cozinha antes que o tempo acabe.\n\n" +
            "<b>Mover:</b> WASD / Setas  -  analógico ou direcional do controle\n" +
            "<b>Dash:</b> Espaço ou Shift  -  botão A / RB (gasta a barra de energia)\n" +
            "<b>Pausar:</b> P ou Esc  -  Start          <b>Som:</b> M  -  Back\n\n" +
            "Cada queijo vale 10 pontos e dá +3 segundos. O relógio dá +10 segundos.\n" +
            "Gatos laranja fazem patrulha. O gato preto persegue você quando chega perto!\n" +
            "Ratoeiras fecham quando você pisa nelas. Você tem 3 vidas.\n" +
            "Quanto mais o tempo passa, mais rápidos os gatos ficam. Sobrou tempo? Vira bônus!";
        Text helpText = CreateText(box.transform, "Texto", help, r.fontBold, 32, Color.white, TextAnchor.UpperLeft);
        helpText.horizontalOverflow = HorizontalWrapMode.Wrap;
        helpText.supportRichText = true;
        SetRect(helpText.rectTransform, Center, Center, Center, new Vector2(0f, -10f), new Vector2(1220f, 520f));
        Button back = CreateButton(box.transform, "Voltar", new Vector2(0f, -320f), r);
        howToPanel.AddComponent<UIFocus>().firstSelected = back;
        howToPanel.SetActive(false);

        MainMenu menu = canvas.gameObject.AddComponent<MainMenu>();
        menu.mainPanel = mainPanel;
        menu.howToPanel = howToPanel;
        menu.playButton = play;
        menu.howToButton = howTo;
        menu.quitButton = quit;
        menu.backButton = back;
        menu.recordText = record;

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // ================================================================== FIM DE JOGO
    static void BuildEndScene(Res r)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(DarkBg, 5f, new Vector3(0f, 0f, -10f));
        Canvas canvas = CreateCanvas("FimDeJogo");
        CreateEventSystem();
        Transform ct = canvas.transform;
        CreateKitchenBackground(ct, r);

        EndScreen end = canvas.gameObject.AddComponent<EndScreen>();
        end.titleText = CreateText(ct, "Titulo", "VITÓRIA!", r.fontBlack, 130, Cheese, TextAnchor.MiddleCenter);
        SetRect(end.titleText.rectTransform, Center, Center, Center, new Vector2(0f, 400f), new Vector2(1600f, 170f));
        end.titleText.GetComponent<Outline>().effectDistance = new Vector2(6f, -6f);
        end.reasonText = CreateText(ct, "Motivo", "", r.fontBold, 44, Color.white, TextAnchor.MiddleCenter);
        SetRect(end.reasonText.rectTransform, Center, Center, Center, new Vector2(0f, 300f), new Vector2(1600f, 70f));

        end.heroImage = CreateImage(ct, "Personagem", r.rato, Color.white);
        end.heroImage.preserveAspect = true;
        SetRect(end.heroImage.rectTransform, Center, Center, Center, new Vector2(0f, 160f), new Vector2(200f, 200f));
        end.winSprite = r.rato;
        end.loseSprite = r.gatoPreto;

        Image statsBox = CreateImage(ct, "CaixaResultados", r.painel, new Color(0.12f, 0.06f, 0.02f, 0.9f));
        statsBox.type = Image.Type.Sliced;
        SetRect(statsBox.rectTransform, Center, Center, Center, new Vector2(0f, -90f), new Vector2(900f, 250f));
        end.statsText = CreateText(statsBox.transform, "Resultados", "", r.fontBold, 44, Color.white, TextAnchor.MiddleCenter);
        end.statsText.lineSpacing = 1.1f;
        SetRect(end.statsText.rectTransform, Vector2.zero, Vector2.one, Center, Vector2.zero, Vector2.zero);
        end.recordText = CreateText(ct, "Recorde", "", r.fontBold, 32, Cheese, TextAnchor.MiddleCenter);
        SetRect(end.recordText.rectTransform, Center, Center, Center, new Vector2(0f, -290f), new Vector2(1600f, 90f));

        end.playAgainButton = CreateButton(ct, "Jogar de Novo", new Vector2(-260f, -410f), r);
        end.menuButton = CreateButton(ct, "Menu", new Vector2(260f, -410f), r);
        canvas.gameObject.AddComponent<UIFocus>().firstSelected = end.playAgainButton;

        EditorSceneManager.SaveScene(scene, EndScenePath);
    }

    // ================================================================== BUILD SETTINGS
    static void ConfigureBuildSettings(bool setIdentity)
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
            new EditorBuildSettingsScene(EndScenePath, true),
        };
        if (setIdentity)
        {
            PlayerSettings.productName = "Rato Guloso";
            PlayerSettings.companyName = AuthorName;
        }
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        // Gzip + fallback: funciona no itch.io sem precisar configurar nada no servidor
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
    }

    // ================================================================== HELPERS DE CENA / UI
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    static Camera CreateCamera(Color background, float size, Vector3 position)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = position;
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        go.AddComponent<AudioListener>();
        return cam;
    }

    static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
        go.AddComponent<UIInputSetup>(); // confere o módulo em tempo de execução
    }

    static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go;
    }

    static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    // atalho: ancorado no topo-centro do pai
    static void SetRect(RectTransform rt, bool top, Vector2 pos, Vector2 size)
    {
        Vector2 a = top ? new Vector2(0.5f, 1f) : Center;
        SetRect(rt, a, a, Center, pos, size);
    }

    static GameObject FullScreenPanel(Transform parent, string name)
    {
        GameObject go = UIObject(name, parent);
        SetRect((RectTransform)go.transform, Vector2.zero, Vector2.one, Center, Vector2.zero, Vector2.zero);
        return go;
    }

    static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject go = UIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text CreateText(Transform parent, string name, string content, Font font, int size, Color color, TextAnchor alignment)
    {
        GameObject go = UIObject(name, parent);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = alignment;
        t.text = content;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.08f, 0.02f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    static Button CreateButton(Transform parent, string label, Vector2 position, Res r)
    {
        GameObject go = UIObject("Botao" + label.Replace(" ", ""), parent);
        SetRect((RectTransform)go.transform, Center, Center, Center, position, new Vector2(440f, 100f));
        Image img = go.AddComponent<Image>();
        img.sprite = r.painel;
        img.type = Image.Type.Sliced;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 0.76f, 0.2f);
        colors.highlightedColor = new Color(1f, 0.88f, 0.45f);
        colors.selectedColor = new Color(1f, 0.95f, 0.7f);
        colors.pressedColor = new Color(0.85f, 0.55f, 0.1f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text text = CreateText(go.transform, "Texto", label, r.fontBlack, 44, Brown, TextAnchor.MiddleCenter);
        Object.DestroyImmediate(text.GetComponent<Outline>());
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Center, Vector2.zero, Vector2.zero);
        return button;
    }

    static void CreateKitchenBackground(Transform parent, Res r)
    {
        Image floor = CreateImage(parent, "Fundo", r.piso, new Color(0.95f, 0.85f, 0.72f));
        floor.type = Image.Type.Tiled;
        SetRect(floor.rectTransform, Vector2.zero, Vector2.one, Center, Vector2.zero, Vector2.zero);
        Image shade = CreateImage(parent, "Escurecer", null, new Color(0.16f, 0.07f, 0.02f, 0.6f));
        SetRect(shade.rectTransform, Vector2.zero, Vector2.one, Center, Vector2.zero, Vector2.zero);
    }

    static void Decoration(Transform parent, string name, Sprite sprite, Color color, Vector2 position, float size, float angle)
    {
        Image img = CreateImage(parent, name, sprite, color);
        img.preserveAspect = true;
        SetRect(img.rectTransform, Center, Center, Center, position, new Vector2(size, size));
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
