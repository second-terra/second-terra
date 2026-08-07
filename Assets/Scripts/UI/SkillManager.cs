using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public Skills skill1;
    public Skills skill2;
    public Skills skill3;
    public Skills skill4;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            skill1.UseSkill();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            skill2.UseSkill();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            skill3.UseSkill();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            skill4.UseSkill();
        }
    }
}
