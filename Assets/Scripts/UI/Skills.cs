using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Skills : MonoBehaviour
{
    public Image cooldownImage;
    public float cooldown = 5f;

    private float timer = 0f;

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;

            if (timer < 0)
                timer = 0;

            cooldownImage.fillAmount = timer / cooldown;
        }
    }

    public void UseSkill()
    {
        if (timer <= 0)
        {
            timer = cooldown;
        }
    }
}
