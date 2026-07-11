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

    [MenuItem("Tools/Second Terra/Create Test Enemies")]
    public static void CreateTestEnemies()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[EnemyTestSetup] Play 모드 중에는 실행할 수 없습니다. 정지 후 다시 실행하세요.");
            return;
        }

        CreateEnemy<MeleeNormalEnemy>("Enemy_Normal", new Vector3(4f, 2f, 0f), new Color(0.9f, 0.2f, 0.2f));
        CreateEnemy<MeleeSuicideEnemy>("Enemy_Suicide", new Vector3(-4f, 2f, 0f), new Color(1f, 0.55f, 0f));
        CreateEnemy<MeleeDashEnemy>("Enemy_Dash", new Vector3(0f, -3f, 0f), new Color(1f, 0.9f, 0.1f));

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnemyTestSetup] 테스트용 적 3종 생성/갱신 완료. File > Save로 씬 저장하세요.");
    }

    private static void CreateEnemy<T>(string name, Vector3 position, Color color) where T : Component
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

            SavePrefab(existing, name);
            return;
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
        SavePrefab(go, name);
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
