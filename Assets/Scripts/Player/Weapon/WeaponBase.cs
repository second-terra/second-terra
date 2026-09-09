using UnityEngine;

// 의체(무기) 공통 베이스. WeaponManager가 다루는 대상을 이 타입으로 제한해서,
// 인스펙터에서 무기가 아닌 컴포넌트가 무기 목록에 들어가는 것을 막는다.
//
// 인터페이스가 아니라 추상 클래스인 이유: Unity는 인터페이스 배열을 인스펙터에
// 직렬화하지 못해서, IWeapon[]으로 두면 무기를 할당할 수 없다.
public abstract class WeaponBase : MonoBehaviour
{
    // 시전 중이라 지금 교체하면 안 되는 상태인지. 시전 시간이 없는 무기는 항상 false.
    public abstract bool IsBusy { get; }

    // HUD 등에 표시할 이름. 기본값은 클래스 이름이고, 필요하면 무기별로 덮어쓴다.
    public virtual string DisplayName => GetType().Name;
}
