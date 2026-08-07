using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    public Image fillImage;

    public float maxHP = 100f;
    public float currentHP = 100f;

    void Update()
    {
        // 체력바 갱신
        fillImage.fillAmount = currentHP / maxHP;

        // 테스트용 (H키를 누르면 체력 10 감소)
        if (Input.GetKeyDown(KeyCode.H))
        {
            currentHP -= 10;

            if (currentHP < 0)
                currentHP = 0;
        }
    }
}
