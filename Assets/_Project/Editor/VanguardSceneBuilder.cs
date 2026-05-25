using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Vanguard.TD.Core;
using Vanguard.TD.Data;
using Vanguard.TD.Economy;
using Vanguard.TD.Gameplay;
using Vanguard.TD.Map;
using Vanguard.TD.UI;
using Vanguard.TD.Waves;

/// <summary>
/// Editor utility — builds the Title and Match scenes from scratch.
/// Layout: top floating badges, BOTTOM action bar, painted sky background.
/// </summary>
public static class VanguardSceneBuilder
{
    // ── Bright-daylight palette ─────────────────────────────────────────────
    static readonly Color SkyFallback = new(0.55f, 0.78f, 0.92f);
    static readonly Color Parchment   = new(0.96f, 0.90f, 0.74f, 0.92f);
    static readonly Color ParchDeep   = new(0.78f, 0.65f, 0.40f, 0.95f);
    static readonly Color InkText     = new(0.20f, 0.13f, 0.07f);
    static readonly Color InkDim      = new(0.40f, 0.30f, 0.18f);
    static readonly Color Gold        = new(0.90f, 0.65f, 0.18f);
    static readonly Color GoldLite    = new(1.00f, 0.85f, 0.30f);
    static readonly Color RedRose     = new(0.85f, 0.25f, 0.28f);
    static readonly Color BlueDeep    = new(0.18f, 0.40f, 0.65f);
    static readonly Color BtnGo       = new(0.30f, 0.70f, 0.32f);
    static readonly Color Wood        = new(0.55f, 0.40f, 0.18f);

    const int BottomBarH = 122;
    const int BadgeH     = 50;

    // ═══════════════════════════════════════════════════════════════════════
    // MENUS
    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem("Vanguard/1 — Build Match scene")]
    static void BuildMatch()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Зупини гру", "Натисни ■ перед збіркою сцени.", "OK"); return;
        }
        EnsureFolder("Assets", "Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ComposeMatch();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Match.unity");
        SyncBuildScenes();
        EditorUtility.DisplayDialog("Vanguard", "Match.unity готова. Натисни ▶ Play.", "OK");
    }

    [MenuItem("Vanguard/2 — Build Title scene")]
    static void BuildTitle()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Зупини гру", "Натисни ■ перед збіркою.", "OK"); return;
        }
        EnsureFolder("Assets", "Scenes");
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ComposeTitle();
        EditorSceneManager.SaveScene(s, "Assets/Scenes/Title.unity");
        SyncBuildScenes();
        if (System.IO.File.Exists("Assets/Scenes/Match.unity"))
            EditorSceneManager.OpenScene("Assets/Scenes/Match.unity");
        EditorUtility.DisplayDialog("Vanguard", "Title.unity готова.", "OK");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MATCH SCENE
    // ═══════════════════════════════════════════════════════════════════════
    static void ComposeMatch()
    {
        ClearScene();
        EnsureAssetFolders();

        var pulse  = MakeTurretBP("Pulse",   "Лучник",   100, 1.0f, 3.0f, 20f, DamageProfile.Direct);
        var plasma = MakeTurretBP("Plasma",  "Маг",      150, 0.6f, 2.0f, 30f, DamageProfile.Splash, splash: 1.5f);
        var cryo   = MakeTurretBP("Cryo",    "Льодяник", 120, 1.0f, 3.0f, 10f, DamageProfile.Chill,  chillF: 0.5f, chillS: 2.0f);
        var rail   = MakeTurretBP("Rail",    "Гармата",  200, 0.3f, 4.5f, 65f, DamageProfile.Direct);

        var scout = MakeHostileBP("Scout", "Гоблін", 10, 40,  3.0f, 1, 5,  false);
        var tank  = MakeHostileBP("Tank",  "Орк",    25, 150, 1.5f, 1, 12, false);
        var phase = MakeHostileBP("Phase", "Привид", 20, 80,  2.0f, 1, 8,  true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Sanity check — log the actual on-disk costs so we can spot regressions
        Debug.Log($"[VanguardBuilder] Turret prices — " +
                  $"Pulse: {pulse.credits}, Plasma: {plasma.credits}, " +
                  $"Cryo: {cryo.credits}, Rail: {rail.credits}");

        // Camera — grid is centered between top badges (~70 px) and bottom bar (122 px)
        var camGo = new GameObject("MainCamera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5.4f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = SkyFallback;
        camGo.transform.position = new Vector3(0.5f, -0.4f, -10f);
        camGo.AddComponent<AudioListener>();

        // Painted sky backdrop (world-space, behind cells); slightly oversized.
        var sky = new GameObject("SkyBackdrop");
        sky.transform.position = new Vector3(0.5f, -0.4f, 8f);
        var skyR = sky.AddComponent<SpriteRenderer>();
        skyR.sprite = BuildDaylightSky();
        skyR.sortingOrder = -10;
        sky.transform.localScale = Vector3.one * 1.15f;

        // Route — triple-S snaking pattern. Enters at top-left, drops, snakes
        // through 3 horizontal lanes, exits at the bottom-right reactor.
        var routePoints = new[]
        {
            new Vector3(-5.5f,  2.5f, 0f),   // 0: entry, top-left
            new Vector3( 3.5f,  2.5f, 0f),   // 1: long run right (top lane)
            new Vector3( 3.5f,  0.5f, 0f),   // 2: drop
            new Vector3(-3.5f,  0.5f, 0f),   // 3: snake back left (mid lane)
            new Vector3(-3.5f, -1.5f, 0f),   // 4: drop
            new Vector3( 3.5f, -1.5f, 0f),   // 5: right again
            new Vector3( 3.5f, -3.5f, 0f),   // 6: drop to bottom lane
            new Vector3( 6.5f, -3.5f, 0f),   // 7: exit to reactor (bottom-right)
        };
        var routeRoot = new GameObject("Route");
        var routeArr  = new Transform[routePoints.Length];
        for (int i = 0; i < routePoints.Length; i++)
        {
            var wp = new GameObject($"R_{i}");
            wp.transform.SetParent(routeRoot.transform);
            wp.transform.position = routePoints[i];
            routeArr[i] = wp.transform;
        }

        // Board
        var boardGo = new GameObject("BoardLayout");
        var board   = boardGo.AddComponent<BoardLayout>();
        board.route = routeArr;

        // Reactor (the defender's base) — bottom-right now
        var reactorGo = new GameObject("Reactor");
        reactorGo.transform.position = new Vector3(6.5f, -3.5f, 0f);
        reactorGo.AddComponent<Reactor>();
        var rsr = reactorGo.AddComponent<SpriteRenderer>();
        rsr.sprite = MakeFlatSprite(GoldLite);
        rsr.sortingOrder = 3;
        reactorGo.transform.localScale = Vector3.one * 0.5f;

        // Entry beacon — top-left
        var entryGo = new GameObject("EntryBeacon");
        entryGo.transform.position = new Vector3(-6.2f, 2.5f, 0f);
        var esr = entryGo.AddComponent<SpriteRenderer>();
        esr.sprite = MakeFlatSprite(RedRose);
        esr.sortingOrder = 3;
        entryGo.transform.localScale = Vector3.one * 0.4f;

        // Range indicator
        var rangeGo = new GameObject("RangeIndicator");
        rangeGo.AddComponent<RangeIndicator>();

        // Systems hub
        var hub = new GameObject("[Systems]");
        hub.AddComponent<GameFlow>();
        hub.AddComponent<GameDirector>();
        hub.AddComponent<CreditLedger>();
        hub.AddComponent<SwarmDirector>();
        hub.AddComponent<BoltPool>();
        hub.AddComponent<ConstructionController>();

        var composer = hub.AddComponent<SwarmComposer>();
        composer.scoutBP = scout; composer.tankBP = tank; composer.phaseBP = phase;

        var pool = hub.AddComponent<HostilePool>();
        var rosterField = typeof(HostilePool).GetField("roster",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        rosterField?.SetValue(pool, new System.Collections.Generic.List<HostilePool.Entry>
        {
            new() { blueprint = scout, preWarm = 15 },
            new() { blueprint = tank,  preWarm = 8  },
            new() { blueprint = phase, preWarm = 8  },
        });

        var boot = hub.AddComponent<BootSequence>();
        boot.pulseBP = pulse; boot.plasmaBP = plasma; boot.cryoBP = cryo; boot.railBP = rail;
        boot.scoutBP = scout; boot.tankBP = tank;   boot.phaseBP = phase;

        // UI
        ComposeMatchUI(pulse, plasma, cryo, rail);

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MATCH UI — top badges + bottom action bar
    // ═══════════════════════════════════════════════════════════════════════
    static void ComposeMatchUI(TurretBlueprint pulse, TurretBlueprint plasma,
                               TurretBlueprint cryo,  TurretBlueprint rail)
    {
        var canvasGo = new GameObject("UI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── 3 floating top badges ───────────────────────────────────────────
        var top = canvasGo.AddComponent<TopBarHud>();
        var goldBadge = BuildBadge(canvasGo.transform, "GoldBadge", 0f,   new Vector2( 24, -14), GoldLite, false);
        var waveBadge = BuildBadge(canvasGo.transform, "WaveBadge", 0.5f, new Vector2(  0, -14), Parchment, true);
        var baseBadge = BuildBadge(canvasGo.transform, "BaseBadge", 1f,   new Vector2(-24, -14), new Color(1f, 0.78f, 0.78f, 0.95f), false);

        top.creditsLabel   = AddBadgeText(goldBadge, "GoldLbl",  "Золото: 300", InkText,  20);
        top.integrityLabel = AddBadgeText(baseBadge, "BaseLbl",  "База: 20 HP", InkText,  20);
        top.roundLabel     = AddBadgeText(waveBadge, "WaveLbl",  "Раунд: 0/10", InkText,  22,  8);
        top.phaseLabel     = AddBadgeText(waveBadge, "PhaseLbl", "[ Підготовка ]", BlueDeep, 14, -12);

        // ── BOTTOM action bar ───────────────────────────────────────────────
        var bar = new GameObject("ActionBar");
        bar.transform.SetParent(canvasGo.transform, false);
        var barRt = bar.AddComponent<RectTransform>();
        barRt.anchorMin = Vector2.zero; barRt.anchorMax = new Vector2(1, 0);
        barRt.pivot = new Vector2(0.5f, 0); barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(0, BottomBarH);
        AddImage(bar, Parchment, raycast: true);
        AddEdge(bar, Wood, 3, top: true);

        // LEFT-side prompt — vertical "ОБЕРИ ВЕЖУ" tag next to the first card
        var prompt = new GameObject("Prompt");
        prompt.transform.SetParent(bar.transform, false);
        var prRt = prompt.AddComponent<RectTransform>();
        prRt.anchorMin = new Vector2(0, 0); prRt.anchorMax = new Vector2(0, 1);
        prRt.pivot = new Vector2(0, 0.5f);
        prRt.anchoredPosition = new Vector2(14, 0);
        prRt.sizeDelta = new Vector2(80, -16);
        var prImg = prompt.AddComponent<Image>();
        prImg.color = new Color(0.30f, 0.18f, 0.08f, 0.92f);  // dark wood plaque
        AddOutline(prompt, Gold, 2);

        var promptLbl = SimpleTMP(prompt.transform, "Lbl", "Обери\nвежу\n↓", 16, GoldLite);
        SetRT(promptLbl.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-4, -4));
        promptLbl.alignment = TextAlignmentOptions.Center;
        promptLbl.fontStyle = FontStyles.Bold;

        // Cards host (between prompt and launch button)
        var cardsHost = new GameObject("Cards");
        cardsHost.transform.SetParent(bar.transform, false);
        var chRt = cardsHost.AddComponent<RectTransform>();
        chRt.anchorMin = new Vector2(0, 0); chRt.anchorMax = new Vector2(1, 1);
        chRt.pivot = new Vector2(0.5f, 0.5f);
        chRt.offsetMin = new Vector2(108, 8);      // 14 (prompt offset) + 80 (prompt width) + 14 gap
        chRt.offsetMax = new Vector2(-270, -8);

        var hlg = cardsHost.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 10;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleCenter;

        var rackUI = canvasGo.AddComponent<TurretRack>();
        rackUI.rackPanel = bar;
        rackUI.pulseBP   = pulse;
        rackUI.plasmaBP  = plasma;
        rackUI.cryoBP    = cryo;
        rackUI.railBP    = rail;

        rackUI.pulseCard  = BuildTurretCard(cardsHost.transform, "PulseCard",  "Лучник",   "100 г", new Color(0.30f, 0.55f, 0.95f));
        rackUI.plasmaCard = BuildTurretCard(cardsHost.transform, "PlasmaCard", "Маг",       "150 г", new Color(0.75f, 0.30f, 0.90f));
        rackUI.cryoCard   = BuildTurretCard(cardsHost.transform, "CryoCard",   "Льодяник", "120 г", new Color(0.25f, 0.75f, 0.95f));
        rackUI.railCard   = BuildTurretCard(cardsHost.transform, "RailCard",   "Гармата",  "200 г", new Color(0.85f, 0.35f, 0.20f));

        // chosenLabel — re-purpose the prompt itself; TurretRack will update it
        rackUI.chosenLabel = promptLbl;
        rackUI.creditsLabel = top.creditsLabel; // gold info also lives in the top badge

        // ── LAUNCH WAVE — big green button on the right of bar ──────────────
        var btnGo = new GameObject("LaunchButton");
        btnGo.transform.SetParent(bar.transform, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1, 0.5f);
        btnRt.anchorMax = new Vector2(1, 0.5f);
        btnRt.pivot     = new Vector2(1, 0.5f);
        btnRt.anchoredPosition = new Vector2(-22, 0);
        btnRt.sizeDelta = new Vector2(230, 88);

        var btnImg = btnGo.AddComponent<Image>(); btnImg.color = BtnGo;
        AddOutline(btnGo, new Color(0.15f, 0.40f, 0.15f), 3);
        var btn = btnGo.AddComponent<Button>();
        Recolor(btn, BtnGo);

        var lbl = new GameObject("Lbl");
        lbl.transform.SetParent(btnGo.transform, false);
        var lblRt = lbl.AddComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = new Vector2(6, 4); lblRt.offsetMax = new Vector2(-6, -4);
        var lblTmp = lbl.AddComponent<TextMeshProUGUI>();
        lblTmp.text = "▶ ПОЧАТИ\nХВИЛЮ";
        lblTmp.fontSize = 22;
        lblTmp.color = Color.white;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.alignment = TextAlignmentOptions.Center;
        btnGo.AddComponent<WaveLaunchButton>();

        bar.SetActive(false); // shown by TurretRack when phase becomes Loadout/Engagement

        // ── Endgame panel ───────────────────────────────────────────────────
        var goPanel = CenterPanel(canvasGo.transform, "EndPanel", new Vector2(580, 340));
        AddImage(goPanel, new Color(0.97f, 0.88f, 0.70f, 0.98f));
        AddOutline(goPanel, Wood, 4);

        var endUI = canvasGo.AddComponent<VictoryPanel>();
        endUI.panel = goPanel;

        endUI.headline = SimpleTMP(goPanel.transform, "Headline", "ПЕРЕМОГА!", 42, RedRose);
        SetRT(endUI.headline.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -60), new Vector2(560, 64));
        endUI.headline.alignment = TextAlignmentOptions.Center;
        endUI.headline.fontStyle = FontStyles.Bold;

        endUI.subtext = SimpleTMP(goPanel.transform, "Subtext",
            "Усі 10 хвиль відбито!", 20, InkText);
        SetRT(endUI.subtext.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 20), new Vector2(500, 36));
        endUI.subtext.alignment = TextAlignmentOptions.Center;

        endUI.replayButton = CenteredButton(goPanel.transform, "ReplayBtn",
            "ГРАТИ ЗНОВУ", BtnGo, new Vector2(0, -110), new Vector2(280, 60));

        goPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TITLE SCENE — fullscreen painted illustration + buttons
    // ═══════════════════════════════════════════════════════════════════════
    static void ComposeTitle()
    {
        var camGo = new GameObject("MainCamera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = SkyFallback;
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        var canvasGo = new GameObject("UI");
        var cv = canvasGo.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = canvasGo.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Fullscreen painted scene
        var bgGo = new GameObject("PaintedScene");
        bgGo.transform.SetParent(canvasGo.transform, false);
        bgGo.transform.SetAsFirstSibling();
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = BuildTitlePainting();
        bgImg.preserveAspect = false;
        bgImg.raycastTarget = false;

        // Title plaque (decorative wooden banner at top)
        var plaque = new GameObject("Plaque");
        plaque.transform.SetParent(canvasGo.transform, false);
        var plRt = plaque.AddComponent<RectTransform>();
        plRt.anchorMin = new Vector2(0.5f, 1); plRt.anchorMax = new Vector2(0.5f, 1);
        plRt.pivot = new Vector2(0.5f, 1);
        plRt.anchoredPosition = new Vector2(0, -40);
        plRt.sizeDelta = new Vector2(820, 130);
        var plImg = plaque.AddComponent<Image>();
        plImg.color = new Color(0.30f, 0.18f, 0.08f, 0.95f);
        AddOutline(plaque, Gold, 4);

        var title = SimpleTMP(plaque.transform, "Title", "TOWER DEFENSE", 64, GoldLite);
        SetRT(title.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -8), new Vector2(800, 70));
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;

        var sub = SimpleTMP(plaque.transform, "Sub", "Захисти базу від навали ворогів", 20,
            new Color(0.98f, 0.92f, 0.72f));
        SetRT(sub.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 18), new Vector2(700, 30));
        sub.alignment = TextAlignmentOptions.Center;

        // Buttons stacked at the BOTTOM, not center
        var ctrl = canvasGo.AddComponent<MainMenuScreen>();
        ctrl.startButton = BottomMenuButton(canvasGo.transform, "StartBtn",  "ГРАТИ",        BtnGo,                       offsetY: 180, w: 320, h: 70, fontSize: 30);
        ctrl.rulesButton = BottomMenuButton(canvasGo.transform, "RulesBtn",  "ПРАВИЛА ГРИ",  new Color(0.50f, 0.35f, 0.18f), offsetY: 90,  w: 320, h: 56, fontSize: 22);

        // Rules panel (parchment scroll)
        var rules = CenterPanel(canvasGo.transform, "RulesPanel", new Vector2(920, 580));
        AddImage(rules, new Color(0.97f, 0.88f, 0.70f, 0.98f));
        AddOutline(rules, Wood, 4);
        ctrl.rulesPanel = rules;

        var rt = SimpleTMP(rules.transform, "RT", "ПРАВИЛА ГРИ", 30, RedRose);
        SetRT(rt.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -30), new Vector2(860, 48));
        rt.alignment = TextAlignmentOptions.Center;
        rt.fontStyle = FontStyles.Bold;

        const string Brief =
@"ІСТОРІЯ
  Орди монстрів рухаються через долину просто
  до твоєї фортеці. Лише грамотний захист стримає
  напад — побудуй стіну з веж і витримай 10 хвиль!

УМОВА ПЕРЕМОГИ
  ✓  База повинна вижити після 10-ї хвилі
  ✗  Кожен ворог, що дійшов — забирає 1 HP бази
  ◆  Стартовий запас — 300 золотих

КЕРУВАННЯ (миша)
  ЛКМ по картці знизу .. обрати вежу
  ЛКМ по зеленій клітинці .. поставити вежу
  ЛКМ по поставленій вежі .. показати радіус
  ПКМ .. скасувати вибір / прибрати радіус

АРСЕНАЛ ВЕЖ
  Лучник  100 g  • стріли, одна ціль, середній радіус
  Маг     150 g  • магічний вибух (AoE), близько
  Льодяник 120 g • -50% швидкості ворогу на 2 с
  Гармата 200 g  • важка шкода, довга дальність

ВОРОЖІ ОДИНИЦІ
  Гоблін .. дрібний і прудкий — легка ціль
  Орк    .. повільний бугай — багато здоров'я
  Привид .. ігнорує уповільнення Льодяника

ПОРАДА
  • Льодяник + Гармата на повороті — комбо
    проти Орків
  • Маг ефективний коли вороги йдуть гурмою
  • Не вкладай усе в одну дорогу — додавай вежі
    далі по шляху для додаткового тиску";

        var rText = SimpleTMP(rules.transform, "Body", Brief, 16, InkText);
        SetRT(rText.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            new Vector2(0, -10), new Vector2(-60, -110));
        rText.alignment = TextAlignmentOptions.TopLeft;
        rText.lineSpacing = 6;

        ctrl.rulesCloseButton = CenteredButton(rules.transform, "Close", "ЗАКРИТИ",
            new Color(0.55f, 0.20f, 0.30f), new Vector2(0, -250), new Vector2(220, 46));
        rules.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PROCEDURAL SPRITES — sky & title illustration
    // ═══════════════════════════════════════════════════════════════════════
    static Sprite BuildDaylightSky()
    {
        int W = 480, H = 270;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        // Sky gradient: brighter on top, slightly warmer at horizon
        for (int y = 0; y < H; y++)
        {
            float t = y / (float)H;
            Color row = Color.Lerp(new Color(0.92f, 0.88f, 0.78f), new Color(0.45f, 0.72f, 0.92f), Mathf.Pow(t, 0.6f));
            for (int x = 0; x < W; x++)
                tex.SetPixel(x, y, row);
        }

        // Sun in upper-right
        int sunCx = (int)(W * 0.78f);
        int sunCy = (int)(H * 0.78f);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(sunCx, sunCy));
            if (d < 26)
            {
                var c = Color.Lerp(new Color(1f, 0.96f, 0.70f), new Color(1f, 0.78f, 0.40f), d / 26f);
                tex.SetPixel(x, y, c);
            }
            else if (d < 50)
            {
                var existing = tex.GetPixel(x, y);
                var glow = new Color(1f, 0.80f, 0.40f, 0.4f);
                tex.SetPixel(x, y, Color.Lerp(existing, glow, 1f - d / 50f));
            }
        }

        // Clouds (lazy puffs)
        var rng = new System.Random(7);
        for (int n = 0; n < 6; n++)
        {
            int cx = rng.Next(40, W - 60);
            int cy = (int)(H * 0.55f) + rng.Next(-10, 30);
            int rw = rng.Next(28, 50);
            int rh = rng.Next(10, 16);
            for (int y = cy - rh; y < cy + rh; y++)
            for (int x = cx - rw; x < cx + rw; x++)
            {
                float dx = (x - cx) / (float)rw;
                float dy = (y - cy) / (float)rh;
                if (dx * dx + dy * dy < 1f)
                {
                    var existing = tex.GetPixel(x, y);
                    var cloud = new Color(1f, 1f, 1f, 0.92f);
                    tex.SetPixel(x, y, Color.Lerp(existing, cloud, 0.85f));
                }
            }
        }

        // Distant mountains silhouette
        int horizon = (int)(H * 0.32f);
        for (int x = 0; x < W; x++)
        {
            float h = Mathf.PerlinNoise(x * 0.02f, 0.5f) * 28 +
                      Mathf.PerlinNoise(x * 0.06f, 1.5f) * 16;
            int mY = horizon + (int)h;
            for (int y = 0; y <= mY; y++)
            {
                var c = Color.Lerp(new Color(0.30f, 0.42f, 0.32f), new Color(0.50f, 0.65f, 0.55f), y / (float)mY);
                tex.SetPixel(x, y, c);
            }
        }

        // Foreground tree silhouettes
        for (int n = 0; n < 12; n++)
        {
            int tx = rng.Next(0, W);
            int treeH = rng.Next(14, 22);
            var trunk = new Color(0.18f, 0.12f, 0.08f);
            for (int y = 0; y < treeH; y++)
            for (int dx = -3; dx <= 3; dx++)
            {
                float radius = 3f - Mathf.Abs(y - treeH * 0.5f) / (treeH * 0.5f) * 1.5f;
                if (Mathf.Abs(dx) < radius)
                    tex.SetPixel(tx + dx, y, trunk);
            }
        }

        tex.Apply();
        // PPU tuned to make the sprite cover ~18 world units wide and 10 tall
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 26);
    }

    static Sprite BuildTitlePainting()
    {
        // Larger illustration for the menu — castle silhouette, sky, two hero figures.
        int W = 640, H = 360;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        // Sky gradient (warmer sunset feel)
        for (int y = 0; y < H; y++)
        {
            float t = y / (float)H;
            Color row = Color.Lerp(
                new Color(0.98f, 0.78f, 0.55f),
                new Color(0.30f, 0.50f, 0.78f),
                Mathf.Pow(t, 0.55f));
            for (int x = 0; x < W; x++)
                tex.SetPixel(x, y, row);
        }

        // Big sun low on the horizon (centered, behind castle)
        int sunCx = W / 2;
        int sunCy = (int)(H * 0.50f);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(sunCx, sunCy));
            if (d < 50)
                tex.SetPixel(x, y, Color.Lerp(new Color(1f, 0.95f, 0.70f), new Color(1f, 0.70f, 0.30f), d / 50f));
            else if (d < 90)
            {
                var existing = tex.GetPixel(x, y);
                var glow = new Color(1f, 0.72f, 0.35f, 0.55f);
                tex.SetPixel(x, y, Color.Lerp(existing, glow, 1f - d / 90f));
            }
        }

        // Distant mountain range (mid-back)
        int horizon = (int)(H * 0.42f);
        for (int x = 0; x < W; x++)
        {
            float h = Mathf.PerlinNoise(x * 0.012f, 0.3f) * 40 +
                      Mathf.PerlinNoise(x * 0.04f, 1.0f) * 24;
            int mY = horizon + (int)h;
            for (int y = 0; y <= mY; y++)
            {
                var c = Color.Lerp(new Color(0.35f, 0.30f, 0.40f), new Color(0.55f, 0.50f, 0.60f), y / (float)mY);
                tex.SetPixel(x, y, c);
            }
        }

        // Foreground ground (green grass)
        int grass = (int)(H * 0.22f);
        for (int x = 0; x < W; x++)
        {
            int gY = grass + (int)(Mathf.PerlinNoise(x * 0.05f, 2.0f) * 8);
            for (int y = 0; y <= gY; y++)
            {
                float n = Mathf.PerlinNoise(x * 0.4f, y * 0.4f);
                var c = Color.Lerp(new Color(0.20f, 0.42f, 0.20f), new Color(0.45f, 0.65f, 0.30f), n);
                tex.SetPixel(x, y, c);
            }
        }

        // CASTLE silhouette in the middle background
        DrawCastleSilhouette(tex, W / 2, (int)(H * 0.36f), 140, 110);

        // HEROES in foreground — small archer (left) and mage (right)
        DrawArcherFigure(tex, (int)(W * 0.22f), (int)(H * 0.18f));
        DrawMageFigure(tex,   (int)(W * 0.78f), (int)(H * 0.18f));

        // A few birds
        var birdInk = new Color(0.12f, 0.08f, 0.10f);
        DrawBird(tex, 120, 280, birdInk);
        DrawBird(tex, 200, 305, birdInk);
        DrawBird(tex, 470, 290, birdInk);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 1);
    }

    // ── Illustration helpers ────────────────────────────────────────────────
    static void DrawCastleSilhouette(Texture2D t, int cx, int by, int width, int height)
    {
        var stoneDark = new Color(0.25f, 0.22f, 0.28f);
        var stoneMid  = new Color(0.42f, 0.38f, 0.45f);
        var roof      = new Color(0.55f, 0.20f, 0.25f);
        var window    = new Color(1f, 0.80f, 0.30f);

        int left = cx - width / 2;
        int right = cx + width / 2;
        int top  = by + height;

        // Side towers
        for (int y = by; y < by + height; y++)
        for (int x = left; x < left + 26; x++)
        {
            bool brick = ((x / 5 + y / 6) % 2 == 0);
            t.SetPixel(x, y, brick ? stoneMid : stoneDark);
        }
        for (int y = by; y < by + height; y++)
        for (int x = right - 26; x < right; x++)
        {
            bool brick = ((x / 5 + y / 6) % 2 == 0);
            t.SetPixel(x, y, brick ? stoneMid : stoneDark);
        }
        // Central keep (taller)
        int keepLeft = cx - 30, keepRight = cx + 30;
        for (int y = by; y < by + height + 20; y++)
        for (int x = keepLeft; x < keepRight; x++)
        {
            bool brick = ((x / 5 + y / 6) % 2 == 0);
            t.SetPixel(x, y, brick ? stoneMid : stoneDark);
        }
        // Gate
        for (int y = by; y < by + 35; y++)
        for (int x = cx - 12; x < cx + 12; x++)
            t.SetPixel(x, y, new Color(0.18f, 0.10f, 0.05f));

        // Conical roofs on side towers
        for (int y = 0; y < 20; y++)
        {
            int spread = 20 - y;
            for (int x = (left + 13) - spread; x <= (left + 13) + spread; x++)
                t.SetPixel(x, by + height + y, roof);
            for (int x = (right - 13) - spread; x <= (right - 13) + spread; x++)
                t.SetPixel(x, by + height + y, roof);
        }
        // Keep roof
        for (int y = 0; y < 26; y++)
        {
            int spread = 26 - y;
            for (int x = cx - spread; x <= cx + spread; x++)
                t.SetPixel(x, by + height + 20 + y, roof);
        }
        // Lit windows
        for (int wy = 1; wy < 4; wy++)
        {
            t.SetPixel(left + 11, by + 50 + wy, window);
            t.SetPixel(left + 12, by + 50 + wy, window);
            t.SetPixel(right - 13, by + 50 + wy, window);
            t.SetPixel(right - 12, by + 50 + wy, window);
            t.SetPixel(cx - 1, by + 80 + wy, window);
            t.SetPixel(cx, by + 80 + wy, window);
            t.SetPixel(cx + 1, by + 80 + wy, window);
        }
        // Flag on keep
        for (int y = 0; y < 12; y++) t.SetPixel(cx, top + 46 + y, new Color(0.2f, 0.1f, 0.05f));
        t.SetPixel(cx + 1, top + 54, new Color(0.95f, 0.25f, 0.2f));
        t.SetPixel(cx + 2, top + 54, new Color(0.95f, 0.25f, 0.2f));
        t.SetPixel(cx + 3, top + 54, new Color(0.95f, 0.25f, 0.2f));
    }

    static void DrawArcherFigure(Texture2D t, int cx, int by)
    {
        var skin = new Color(0.95f, 0.78f, 0.62f);
        var hair = new Color(0.42f, 0.25f, 0.10f);
        var tunic = new Color(0.20f, 0.45f, 0.20f);
        var leather = new Color(0.40f, 0.28f, 0.15f);
        var bow = new Color(0.60f, 0.40f, 0.18f);

        // Legs
        for (int y = 0; y < 14; y++)
        {
            t.SetPixel(cx - 4, by + y, leather);
            t.SetPixel(cx - 3, by + y, leather);
            t.SetPixel(cx + 2, by + y, leather);
            t.SetPixel(cx + 3, by + y, leather);
        }
        // Body
        for (int y = 14; y < 32; y++)
        for (int x = cx - 7; x <= cx + 7; x++)
            t.SetPixel(x, by + y, tunic);
        // Head
        for (int y = 32; y < 44; y++)
        for (int x = cx - 5; x <= cx + 5; x++)
        {
            int dx = x - cx, dy = y - 38;
            if (dx * dx + dy * dy < 30) t.SetPixel(x, by + y, skin);
        }
        // Hair
        for (int x = cx - 5; x <= cx + 5; x++)
            t.SetPixel(x, by + 42, hair);
        // Eye
        t.SetPixel(cx - 2, by + 38, new Color(0.05f, 0.05f, 0.05f));
        t.SetPixel(cx + 2, by + 38, new Color(0.05f, 0.05f, 0.05f));

        // Bow (held to the side)
        for (int y = 14; y < 36; y++)
        {
            t.SetPixel(cx + 11, by + y, bow);
        }
        // bowstring
        for (int y = 14; y < 36; y++)
            t.SetPixel(cx + 12, by + y, new Color(0.95f, 0.95f, 0.95f, 0.7f));
    }

    static void DrawMageFigure(Texture2D t, int cx, int by)
    {
        var skin = new Color(0.95f, 0.80f, 0.65f);
        var robe = new Color(0.40f, 0.20f, 0.60f);
        var hatHi = new Color(0.55f, 0.30f, 0.80f);
        var staff = new Color(0.55f, 0.35f, 0.18f);
        var orb = new Color(1f, 0.85f, 0.30f);

        // Robe (cone shape)
        for (int y = 0; y < 28; y++)
        {
            int half = 4 + y / 4;
            for (int x = cx - half; x <= cx + half; x++)
                t.SetPixel(x, by + y, robe);
        }
        // Face
        for (int y = 28; y < 40; y++)
        for (int x = cx - 5; x <= cx + 5; x++)
        {
            int dx = x - cx, dy = y - 34;
            if (dx * dx + dy * dy < 30) t.SetPixel(x, by + y, skin);
        }
        // Pointy hat
        for (int y = 40; y < 56; y++)
        {
            int half = (56 - y) / 2;
            for (int x = cx - half; x <= cx + half; x++)
                t.SetPixel(x, by + y, robe);
        }
        // Hat brim
        for (int x = cx - 8; x <= cx + 8; x++)
            t.SetPixel(x, by + 40, hatHi);

        // Staff
        for (int y = 0; y < 50; y++)
            t.SetPixel(cx - 11, by + y, staff);
        // Orb on staff
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(4, 4));
            if (d < 4) t.SetPixel(cx - 14 + x, by + 46 + y, orb);
        }
        // Beard
        for (int y = 26; y < 32; y++)
        for (int x = cx - 4; x <= cx + 4; x++)
            t.SetPixel(x, by + y, new Color(0.92f, 0.92f, 0.92f));
        // Eyes
        t.SetPixel(cx - 2, by + 36, new Color(0.05f, 0.05f, 0.05f));
        t.SetPixel(cx + 2, by + 36, new Color(0.05f, 0.05f, 0.05f));
    }

    static void DrawBird(Texture2D t, int x, int y, Color c)
    {
        // V-shaped tiny bird
        t.SetPixel(x - 2, y + 1, c); t.SetPixel(x - 1, y, c);
        t.SetPixel(x,     y - 1, c);
        t.SetPixel(x + 1, y, c); t.SetPixel(x + 2, y + 1, c);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FACTORIES — ScriptableObjects
    // ═══════════════════════════════════════════════════════════════════════
    static TurretBlueprint MakeTurretBP(string fileName, string displayName, int cost,
        float rof, float reach, float dmg, DamageProfile prof,
        float splash = 0f, float chillF = 0f, float chillS = 0f)
    {
        string path = $"Assets/ScriptableObjects/Turrets/{fileName}.asset";

        // Always start clean — delete any stale .asset on disk so default
        // values from old builds don't leak into the new one.
        if (AssetDatabase.LoadAssetAtPath<TurretBlueprint>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var bp = ScriptableObject.CreateInstance<TurretBlueprint>();
        bp.codename     = displayName;
        bp.credits      = cost;
        bp.rateOfFire   = rof;
        bp.reach        = reach;
        bp.damage       = dmg;
        bp.profile      = prof;
        bp.turretPrefab = null;
        bp.boltPrefab   = null;
        bp.splashRadius = splash;
        bp.chillFactor  = chillF;
        bp.chillSeconds = chillS;
        AssetDatabase.CreateAsset(bp, path);
        EditorUtility.SetDirty(bp);
        return bp;
    }

    static HostileBlueprint MakeHostileBP(string fileName, string displayName, int cost,
        int hp, float pace, int reactorDmg, int payout, bool chillImmune)
    {
        string path = $"Assets/ScriptableObjects/Hostiles/{fileName}.asset";

        if (AssetDatabase.LoadAssetAtPath<HostileBlueprint>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var bp = ScriptableObject.CreateInstance<HostileBlueprint>();
        bp.codename      = displayName;
        bp.budgetCost    = cost;
        bp.prefab        = null;
        bp.maxIntegrity  = hp;
        bp.pace          = pace;
        bp.reactorDamage = reactorDmg;
        bp.payout        = payout;
        bp.chillImmune   = chillImmune;
        AssetDatabase.CreateAsset(bp, path);
        EditorUtility.SetDirty(bp);
        return bp;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UI HELPERS
    // ═══════════════════════════════════════════════════════════════════════
    static GameObject CenterPanel(Transform p, string n, Vector2 sz)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = sz;
        return go;
    }

    static void SetRT(RectTransform rt, Vector2 mn, Vector2 mx, Vector2 piv, Vector2 pos, Vector2 sz)
    {
        rt.anchorMin = mn; rt.anchorMax = mx; rt.pivot = piv;
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
    }

    static TextMeshProUGUI SimpleTMP(Transform p, string n, string txt, float sz, Color c)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = sz; t.color = c;
        return t;
    }

    static GameObject BuildBadge(Transform parent, string name, float anchorX,
                                 Vector2 offset, Color tint, bool wide)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorX, 1f);
        rt.anchorMax = new Vector2(anchorX, 1f);
        rt.pivot     = new Vector2(anchorX, 1f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = wide ? new Vector2(290, 68) : new Vector2(230, BadgeH);
        var img = go.AddComponent<Image>();
        img.color = tint;
        AddOutline(go, new Color(0.55f, 0.40f, 0.15f, 0.9f), 2);
        return go;
    }

    static TextMeshProUGUI AddBadgeText(GameObject badge, string name, string txt,
                                         Color col, float size, float yOffset = 0)
    {
        var go = new GameObject(name);
        go.transform.SetParent(badge.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8, yOffset - 4);
        rt.offsetMax = new Vector2(-8, yOffset + 4);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt; t.fontSize = size; t.color = col;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        return t;
    }

    static Button BuildTurretCard(Transform parent, string name, string title, string price, Color accent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 116; le.preferredHeight = 96;
        le.flexibleWidth = 0; le.flexibleHeight = 0;

        var img = go.AddComponent<Image>();
        img.color = ParchDeep;
        var btn = go.AddComponent<Button>();
        Recolor(btn, accent);
        AddOutline(go, new Color(0.55f, 0.40f, 0.15f, 0.85f), 2);

        // Accent stripe on left
        var stripe = new GameObject("Stripe");
        stripe.transform.SetParent(go.transform, false);
        var sRt = stripe.AddComponent<RectTransform>();
        sRt.anchorMin = Vector2.zero; sRt.anchorMax = new Vector2(0, 1);
        sRt.pivot = new Vector2(0, 0.5f);
        sRt.sizeDelta = new Vector2(10, 0);
        sRt.anchoredPosition = Vector2.zero;
        var stImg = stripe.AddComponent<Image>();
        stImg.color = accent; stImg.raycastTarget = false;

        // Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(go.transform, false);
        var tRt = titleGo.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0, 0.45f); tRt.anchorMax = new Vector2(1, 1);
        tRt.offsetMin = new Vector2(14, 0); tRt.offsetMax = new Vector2(-4, -4);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title; titleTmp.fontSize = 18; titleTmp.color = InkText;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        // Price
        var priceGo = new GameObject("Price");
        priceGo.transform.SetParent(go.transform, false);
        var pRt = priceGo.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0, 0); pRt.anchorMax = new Vector2(1, 0.45f);
        pRt.offsetMin = new Vector2(14, 4); pRt.offsetMax = new Vector2(-4, 0);
        var priceTmp = priceGo.AddComponent<TextMeshProUGUI>();
        priceTmp.text = price; priceTmp.fontSize = 16; priceTmp.color = new Color(0.40f, 0.25f, 0.10f);
        priceTmp.fontStyle = FontStyles.Bold;
        priceTmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    static Button BottomMenuButton(Transform parent, string n, string lbl, Color bg,
                                   float offsetY, float w, float h, float fontSize)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, offsetY);
        rt.sizeDelta = new Vector2(w, h);
        var img = go.AddComponent<Image>(); img.color = bg;
        AddOutline(go, new Color(0.18f, 0.10f, 0.05f), 3);
        var btn = go.AddComponent<Button>(); Recolor(btn, bg);

        var lblGo = new GameObject("Lbl"); lblGo.transform.SetParent(go.transform, false);
        var lRt = lblGo.AddComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var t = lblGo.AddComponent<TextMeshProUGUI>();
        t.text = lbl; t.fontSize = fontSize; t.color = Color.white;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    static Button CenteredButton(Transform p, string n, string lbl, Color bg, Vector2 pos, Vector2 sz)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
        var img = go.AddComponent<Image>(); img.color = bg;
        AddOutline(go, new Color(0.18f, 0.10f, 0.05f), 2);
        var btn = go.AddComponent<Button>(); Recolor(btn, bg);

        var lblGo = new GameObject("Lbl"); lblGo.transform.SetParent(go.transform, false);
        var lRt = lblGo.AddComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var t = lblGo.AddComponent<TextMeshProUGUI>();
        t.text = lbl; t.fontSize = 22; t.color = Color.white;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    static void Recolor(Button b, Color c)
    {
        var cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = c;
        cb.highlightedColor = c * 1.35f;
        cb.pressedColor     = c * 0.65f;
        cb.selectedColor    = c;
        cb.colorMultiplier  = 1f;
        b.colors = cb;
    }

    static Image AddImage(GameObject go, Color c, bool raycast = true)
    {
        var i = go.AddComponent<Image>();
        i.color = c;
        i.raycastTarget = raycast;
        return i;
    }

    static void AddOutline(GameObject go, Color c, float w = 2)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(w, w);
    }

    static void AddEdge(GameObject parent, Color c, float thickness,
                        bool top = false, bool bottom = false, bool left = false, bool right = false)
    {
        var ch = new GameObject("Edge"); ch.transform.SetParent(parent.transform, false);
        var rt = ch.AddComponent<RectTransform>();
        if (bottom)      { rt.anchorMin = Vector2.zero;        rt.anchorMax = new Vector2(1, 0);
                           rt.pivot = new Vector2(0.5f, 0);    rt.sizeDelta = new Vector2(0, thickness); }
        else if (top)    { rt.anchorMin = new Vector2(0, 1);   rt.anchorMax = Vector2.one;
                           rt.pivot = new Vector2(0.5f, 1);    rt.sizeDelta = new Vector2(0, thickness); }
        else if (left)   { rt.anchorMin = Vector2.zero;        rt.anchorMax = new Vector2(0, 1);
                           rt.pivot = new Vector2(0, 0.5f);    rt.sizeDelta = new Vector2(thickness, 0); }
        else if (right)  { rt.anchorMin = new Vector2(1, 0);   rt.anchorMax = Vector2.one;
                           rt.pivot = new Vector2(1, 0.5f);    rt.sizeDelta = new Vector2(thickness, 0); }
        rt.anchoredPosition = Vector2.zero;
        var img = ch.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
    }

    static Sprite MakeFlatSprite(Color c)
    {
        var t = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = c;
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUILD SETTINGS / FOLDERS / CLEAN
    // ═══════════════════════════════════════════════════════════════════════
    static void SyncBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Match.unity", true),
        };
    }

    static void EnsureAssetFolders()
    {
        EnsureFolder("Assets", "ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects", "Turrets");
        EnsureFolder("Assets/ScriptableObjects", "Hostiles");
    }

    static void EnsureFolder(string parent, string n)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{n}"))
            AssetDatabase.CreateFolder(parent, n);
    }

    static void ClearScene()
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().GetRootGameObjects())
            Object.DestroyImmediate(go);
    }
}
