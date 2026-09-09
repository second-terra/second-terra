using UnityEngine;

// 무기(의체) 교체 관리자. 무기 3종이 좌클릭과 1~4 키를 공유하기 때문에,
// 한 번에 하나만 활성화해서 입력이 겹치지 않도록 한다.
// 각 무기는 OnDisable에서 자기 상태를 원복하므로 컴포넌트를 켜고 끄는 것만으로 교체가 안전하다.
public class WeaponManager : MonoBehaviour
{
    [Header("무기 목록 (인스펙터 순서 = 의체 선택 순서)")]
    [Tooltip("대검 / 기관단총 / 드론 순으로 넣어주세요. 의체 선택창의 인덱스와 순서가 맞아야 합니다.")]
    [SerializeField] private WeaponBase[] weapons;

    [Header("교체 키")]
    [SerializeField] private KeyCode prevKey = KeyCode.Q;
    [SerializeField] private KeyCode nextKey = KeyCode.E;
    [SerializeField] private bool useScrollWheel = true;

    [Header("의체 선택 연동")]
    [Tooltip("의체 선택창에서 저장한 PlayerPrefs 값을 읽어 시작 무기를 정합니다.")]
    [SerializeField] private bool useSelectedMech = true;
    [SerializeField] private string selectedMechKey = "SelectedMech";
    [SerializeField] private int defaultWeaponIndex = 0;

    [Header("개발용 HUD")]
    [SerializeField] private bool showDebugHUD = true;

    private int currentIndex = -1;

    public int CurrentIndex => currentIndex;
    public int WeaponCount => weapons != null ? weapons.Length : 0;
    public WeaponBase CurrentWeapon =>
        IsValidIndex(currentIndex) ? weapons[currentIndex] : null;

    // 현재 무기가 시전 중이라 교체하면 안 되는 상태인지
    public bool IsCurrentBusy => CurrentWeapon != null && CurrentWeapon.IsBusy;

    private void Awake()
    {
        // 무기들이 각자 OnEnable을 돌리기 전에 우선 다 꺼둔다.
        // (컴포넌트 실행 순서가 보장되지 않아 Start에서 한 번 더 정리한다)
        DisableAll();
    }

    private void Start()
    {
        int index = defaultWeaponIndex;

        if (useSelectedMech && PlayerPrefs.HasKey(selectedMechKey))
        {
            int saved = PlayerPrefs.GetInt(selectedMechKey);
            if (IsValidIndex(saved))
                index = saved;
            else
                Debug.LogWarning($"[WeaponManager] 저장된 의체 인덱스({saved})가 무기 목록 범위를 벗어나 기본값을 사용합니다.");
        }

        // 기본 인덱스마저 비어있으면 무기가 하나도 안 켜진 채 시작하므로, 첫 유효 슬롯으로 대체한다.
        if (!IsValidIndex(index))
        {
            index = FindFirstValidIndex();
            if (index < 0)
            {
                Debug.LogWarning("[WeaponManager] 등록된 무기가 없습니다. 인스펙터에서 무기 목록을 확인해주세요.");
                return;
            }
        }

        Equip(index);
    }

    private int FindFirstValidIndex()
    {
        for (int i = 0; i < WeaponCount; i++)
            if (weapons[i] != null) return i;
        return -1;
    }

    private void Update()
    {
        if (WeaponCount <= 1) return;

        // 키와 휠이 같은 프레임에 겹치면 두 칸 넘어가므로, 한 프레임에 한 번만 교체한다.
        if (Input.GetKeyDown(nextKey)) { Cycle(1); return; }
        if (Input.GetKeyDown(prevKey)) { Cycle(-1); return; }

        if (useScrollWheel)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) Cycle(1);
            else if (scroll < 0f) Cycle(-1);
        }
    }

    // 지정한 무기만 켜고 나머지는 끈다. 범위를 벗어나면 아무것도 하지 않는다.
    public void Equip(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogWarning($"[WeaponManager] 무기 인덱스 {index}가 유효하지 않습니다. (무기 {WeaponCount}개)");
            return;
        }

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null) continue;
            weapons[i].enabled = (i == index);
        }

        currentIndex = index;
    }

    // 비어있는 슬롯은 건너뛰고 다음/이전 무기로 순환한다.
    // 시전 중인 무기는 교체하지 않는다 (쿨타임만 소모하고 타격이 안 나가는 것을 막기 위해).
    public void Cycle(int direction)
    {
        if (WeaponCount == 0) return;
        if (IsCurrentBusy) return;

        // 아직 아무것도 장착 못 한 상태(-1)면 방향과 무관하게 첫 유효 슬롯으로 간다.
        if (currentIndex < 0)
        {
            int first = FindFirstValidIndex();
            if (first >= 0) Equip(first);
            return;
        }

        for (int step = 1; step <= weapons.Length; step++)
        {
            int next = Mod(currentIndex + direction * step, weapons.Length);
            if (weapons[next] != null)
            {
                Equip(next);
                return;
            }
        }
    }

    private void DisableAll()
    {
        if (weapons == null) return;
        foreach (var w in weapons)
            if (w != null) w.enabled = false;
    }

    private bool IsValidIndex(int index) =>
        weapons != null && index >= 0 && index < weapons.Length && weapons[index] != null;

    // C#의 %는 음수에서 음수를 반환하므로 항상 0 이상이 되도록 보정한다.
    private static int Mod(int value, int length) => ((value % length) + length) % length;

    private GUIStyle hudStyle;

    private void OnGUI()
    {
        if (!showDebugHUD) return;

        // OnGUI는 한 프레임에 여러 번(Layout/Repaint) 호출되므로 매번 새로 만들지 않는다.
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            hudStyle.normal.textColor = Color.white;
        }
        GUIStyle style = hudStyle;

        string name = CurrentWeapon != null ? CurrentWeapon.DisplayName : "없음";
        string swap = IsCurrentBusy ? "시전 중 (교체 불가)" : $"{prevKey}/{nextKey} 또는 휠로 교체";
        GUI.Label(new Rect(14, Screen.height - 40, 620, 30),
            $"[무기] {name}  ({currentIndex + 1}/{WeaponCount})   {swap}", style);
    }
}
