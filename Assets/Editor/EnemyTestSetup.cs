using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EnemyTestSetup
{
    private const string PrefabFolder = "Assets/Prefabs";

    private static Sprite cachedSprite;

    [MenuItem("Tools/Second Terra/Force Stop Play Mode")]
    public static void ForceStopPlayMode()
    {
        EditorApplication.isPlaying = false;
    }

    [MenuItem("Tools/Second Terra/Setup Player Collider")]
    public static void SetupPlayerCollider()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[EnemyTestSetup] Play 모드 중에는 실행할 수 없습니다.");
            return;
        }

        var playerStats = Object.FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("[EnemyTestSetup] 씬에서 PlayerStats를 찾지 못했습니다.");
            return;
        }

        var player = playerStats.gameObject;
        if (player.GetComponent<Collider2D>() != null)
        {
            Debug.Log("[EnemyTestSetup] Player에 이미 Collider2D가 있습니다.");
            return;
        }

        var col = Undo.AddComponent<CircleCollider2D>(player);
        col.radius = 0.5f;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnemyTestSetup] Player에 CircleCollider2D 추가 완료. File > Save로 씬 저장하세요.");
    }

    [MenuItem("Tools/Second Terra/Setup Player Health Bar")]
    public static void SetupPlayerHealthBar()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[EnemyTestSetup] Play 모드 중에는 실행할 수 없습니다.");
            return;
        }

        // 기존 PlayerHUD 캔버스가 있으면 제거 후 재생성
        var existing = GameObject.Find("PlayerHUD");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        // Screen Space - Overlay 캔버스 생성
        var canvasGo = new GameObject("PlayerHUD");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create PlayerHUD");

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 체력바 루트 오브젝트
        var hpBarRoot = new GameObject("HpBar");
        hpBarRoot.transform.SetParent(canvasGo.transform, false);
        var rootRect = hpBarRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(20f, -20f);
        rootRect.sizeDelta = new Vector2(300f, 30f);

        // 배경
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(hpBarRoot.transform, false);
        var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);
        SetFullRect(bgImage.rectTransform);

        // Fill Area
        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(hpBarRoot.transform, false);
        var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2, 2);
        fillAreaRect.offsetMax = new Vector2(-2, -2);

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillImage = fillGo.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = new Color(0.9f, 0.15f, 0.15f);
        SetFullRect(fillImage.rectTransform);

        // Slider
        var slider = hpBarRoot.AddComponent<UnityEngine.UI.Slider>();
        slider.interactable = false;
        slider.transition = UnityEngine.UI.Selectable.Transition.None;
        slider.fillRect = fillImage.rectTransform;
        slider.handleRect = null;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // PlayerHealthBar 컴포넌트
        var healthBar = hpBarRoot.AddComponent<PlayerHealthBar>();
        var so = new SerializedObject(healthBar);
        so.FindProperty("hpSlider").objectReferenceValue = slider;

        // PlayerStats 자동 연결
        var playerStats = Object.FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            so.FindProperty("playerStats").objectReferenceValue = playerStats;

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnemyTestSetup] 플레이어 체력바 생성 완료. File > Save로 씬 저장하세요.");
    }

    [MenuItem("Tools/Second Terra/Create Test Enemies")]
    public static void CreateTestEnemies()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[EnemyTestSetup] Play 모드 중에는 실행할 수 없습니다. 정지 후 다시 실행하세요.");
            return;
        }

        var normal = CreateEnemy<MeleeNormalStationaryEnemy>("Enemy_Normal", new Vector3(4f, 2f, 0f), new Color(0.9f, 0.2f, 0.2f));
        SavePrefab(normal, "Enemy_Normal");

        var normalMoving = CreateEnemy<MeleeNormalMovingEnemy>("Enemy_Normal_Moving", new Vector3(2f, 4f, 0f), new Color(1f, 0.6f, 0.75f));
        SavePrefab(normalMoving, "Enemy_Normal_Moving");

        var suicide = CreateEnemy<MeleeSuicideEnemy>("Enemy_Suicide", new Vector3(-4f, 2f, 0f), new Color(1f, 0.55f, 0f));
        SavePrefab(suicide, "Enemy_Suicide");

        var dash = CreateEnemy<MeleeDashEnemy>("Enemy_Dash", new Vector3(0f, -3f, 0f), new Color(1f, 0.9f, 0.1f));
        SavePrefab(dash, "Enemy_Dash");

        var enemyProjectilePrefab = GetOrCreateEnemyProjectilePrefab();

        var barrageRandomRadial = CreateEnemy<RangedBarrageEnemy>("Enemy_Barrage_RandomRadial", new Vector3(6f, -1f, 0f), new Color(0.05f, 0.4f, 0.1f));
        ConfigureBarrage(barrageRandomRadial, RangedBarrageEnemy.AimMode.Random, RangedBarrageEnemy.FirePattern.Radial, enemyProjectilePrefab);
        SavePrefab(barrageRandomRadial, "Enemy_Barrage_RandomRadial");

        var barrageMoveStraight = CreateEnemy<RangedBarrageEnemy>("Enemy_Barrage_MoveStraight", new Vector3(-6f, -1f, 0f), new Color(0.6f, 0.95f, 0.6f));
        ConfigureBarrage(barrageMoveStraight, RangedBarrageEnemy.AimMode.PlayerMoveDirection, RangedBarrageEnemy.FirePattern.Straight, enemyProjectilePrefab);
        SavePrefab(barrageMoveStraight, "Enemy_Barrage_MoveStraight");

        var beam = CreateEnemy<RangedBeamEnemy>("Enemy_Beam", new Vector3(0f, 5f, 0f), new Color(0.1f, 0.4f, 0.95f));
        SavePrefab(beam, "Enemy_Beam");

        var zoneRandom = CreateEnemy<RangedZoneEnemy>("Enemy_Zone_Random", new Vector3(6f, 3f, 0f), new Color(0.55f, 0.15f, 0.85f));
        ConfigureZone(zoneRandom, RangedZoneEnemy.ZoneLocation.RandomArea);
        SavePrefab(zoneRandom, "Enemy_Zone_Random");

        var zoneAroundPlayer = CreateEnemy<RangedZoneEnemy>("Enemy_Zone_AroundPlayer", new Vector3(-6f, 3f, 0f), new Color(0.8f, 0.65f, 0.95f));
        ConfigureZone(zoneAroundPlayer, RangedZoneEnemy.ZoneLocation.AroundPlayer);
        SavePrefab(zoneAroundPlayer, "Enemy_Zone_AroundPlayer");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnemyTestSetup] 테스트용 적 9종(근접 4 + 원거리 5) 생성/갱신 완료. File > Save로 씬 저장하세요.");
    }

    [MenuItem("Tools/Second Terra/Create Test Boss - Sector1")]
    public static void CreateTestBossSector1()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[EnemyTestSetup] Play 모드 중에는 실행할 수 없습니다. 정지 후 다시 실행하세요.");
            return;
        }

        var projectilePrefab = GetOrCreateEnemyProjectilePrefab();

        CreateEnemy<Sector1Boss>("Boss_Sector1", new Vector3(0f, -6f, 0f), new Color(0.75f, 0.75f, 0.78f));

        var boss = GameObject.Find("Boss_Sector1");
        var so = new SerializedObject(boss.GetComponent<Sector1Boss>());
        so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(boss, "Boss_Sector1");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnemyTestSetup] 1섹터 보스 테스트 생성 완료. File > Save로 씬 저장하세요.");
    }

    [MenuItem("Tools/Second Terra/Create Test Boss - Sector2")]
    public static void CreateTestBossSector2()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[EnemyTestSetup] Play 모드 중에는 실행할 수 없습니다. 정지 후 다시 실행하세요.");
            return;
        }

        var projectilePrefab = GetOrCreateEnemyProjectilePrefab();

        CreateEnemy<Sector2Boss>("Boss_Sector2", new Vector3(0f, -8f, 0f), new Color(0.2f, 0.2f, 0.22f));

        var boss = GameObject.Find("Boss_Sector2");
        var so = new SerializedObject(boss.GetComponent<Sector2Boss>());
        so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(boss, "Boss_Sector2");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnemyTestSetup] 2섹터 보스 테스트 생성 완료. File > Save로 씬 저장하세요.");
    }

    [MenuItem("Tools/Second Terra/Create Test Boss - Sector3")]
    public static void CreateTestBossSector3()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[EnemyTestSetup] Play 모드 중에는 실행할 수 없습니다. 정지 후 다시 실행하세요.");
            return;
        }

        var summonedAddsPrefab = GetOrCreateSummonedAddsPrefab();
        var trailProjectilePrefab = GetOrCreateTrailProjectilePrefab();

        CreateEnemy<Sector3Boss>("Boss_Sector3", new Vector3(0f, -10f, 0f), new Color(0.07f, 0.07f, 0.08f));

        var boss = GameObject.Find("Boss_Sector3");
        var so = new SerializedObject(boss.GetComponent<Sector3Boss>());
        so.FindProperty("summonedAddsPrefab").objectReferenceValue = summonedAddsPrefab;
        so.FindProperty("trailProjectilePrefab").objectReferenceValue = trailProjectilePrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(boss, "Boss_Sector3");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnemyTestSetup] 3섹터 보스 테스트 생성 완료. File > Save로 씬 저장하세요.");
    }

    private static GameObject GetOrCreateSummonedAddsPrefab()
    {
        string path = $"{PrefabFolder}/SummonedAdds.prefab";
        var existingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existingAsset != null)
            return existingAsset;

        var summonedAdds = CreateEnemy<SummonedAdds>("SummonedAdds", new Vector3(0f, -10.5f, 0f), new Color(0.55f, 0.6f, 0.35f));
        SavePrefab(summonedAdds, "SummonedAdds");
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static GameObject GetOrCreateTrailProjectilePrefab()
    {
        string path = $"{PrefabFolder}/TrailProjectile.prefab";
        var existingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existingAsset != null)
            return existingAsset;

        var go = new GameObject("TrailProjectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetPlaceholderSprite();
        sr.color = new Color(0.3f, 0.9f, 0.3f);
        go.transform.localScale = Vector3.one * 0.25f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        go.AddComponent<TrailProjectile>();

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var savedAsset = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return savedAsset;
    }

    private static GameObject CreateEnemy<T>(string name, Vector3 position, Color color) where T : Component
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            existing.transform.position = position;
            var existingSr = existing.GetComponent<SpriteRenderer>();
            if (existingSr != null)
                existingSr.color = color;

            if (existing.GetComponentInChildren<EnemyHealthBar>(true) == null)
                AddHealthBar(existing);

            if (existing.GetComponent<Collider2D>() == null)
                AddTriggerCollider(existing);

            return existing;
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetPlaceholderSprite();
        sr.color = color;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        AddTriggerCollider(go);
        go.AddComponent<T>();
        AddHealthBar(go);

        Selection.activeGameObject = go;
        return go;
    }

    private static GameObject GetOrCreateEnemyProjectilePrefab()
    {
        string path = $"{PrefabFolder}/EnemyProjectile.prefab";
        var existingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existingAsset != null)
            return existingAsset;

        var go = new GameObject("EnemyProjectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetPlaceholderSprite();
        sr.color = new Color(0.7f, 0.2f, 1f);
        go.transform.localScale = Vector3.one * 0.3f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        go.AddComponent<EnemyProjectile>();

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var savedAsset = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return savedAsset;
    }

    private static void ConfigureBarrage(GameObject go, RangedBarrageEnemy.AimMode aimMode, RangedBarrageEnemy.FirePattern firePattern, GameObject projectilePrefab)
    {
        var component = go.GetComponent<RangedBarrageEnemy>();
        if (component == null)
        {
            Debug.LogWarning($"[EnemyTestSetup] '{go.name}'에 RangedBarrageEnemy 컴포넌트가 없어 설정을 건너뜁니다.");
            return;
        }

        var so = new SerializedObject(component);
        so.FindProperty("aimMode").enumValueIndex = (int)aimMode;
        so.FindProperty("firePattern").enumValueIndex = (int)firePattern;
        so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureZone(GameObject go, RangedZoneEnemy.ZoneLocation zoneLocation)
    {
        var component = go.GetComponent<RangedZoneEnemy>();
        if (component == null)
        {
            Debug.LogWarning($"[EnemyTestSetup] '{go.name}'에 RangedZoneEnemy 컴포넌트가 없어 설정을 건너뜁니다.");
            return;
        }

        var so = new SerializedObject(component);
        so.FindProperty("zoneLocation").enumValueIndex = (int)zoneLocation;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SavePrefab(GameObject go, string name)
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string path = $"{PrefabFolder}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
    }

    private static void AddTriggerCollider(GameObject enemy)
    {
        var col = enemy.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
    }

    private static void AddHealthBar(GameObject enemy)
    {
        var canvasGo = new GameObject("HealthBarCanvas");
        canvasGo.transform.SetParent(enemy.transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;
        var canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 30);
        canvasRect.localScale = Vector3.one * 0.01f;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);
        SetFullRect(bgImage.rectTransform);

        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(canvasGo.transform, false);
        var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2, 2);
        fillAreaRect.offsetMax = new Vector2(-2, -2);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillImage = fillGo.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.9f, 0.2f);
        SetFullRect(fillImage.rectTransform);

        var slider = canvasGo.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fillImage.rectTransform;
        slider.handleRect = null;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        var healthBar = canvasGo.AddComponent<EnemyHealthBar>();
        var so = new SerializedObject(healthBar);
        so.FindProperty("worldCanvas").objectReferenceValue = canvas;
        so.FindProperty("hpSlider").objectReferenceValue = slider;
        so.FindProperty("yOffset").floatValue = 0.9f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFullRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite GetPlaceholderSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        const int size = 32;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.name = "EnemyPlaceholderTex";

        cachedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cachedSprite.name = "EnemyPlaceholderSprite";
        return cachedSprite;
    }
}
