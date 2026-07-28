using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public MissionUI mission1;

    public MissionUI mission2;

    public MissionUI bossMission;

    void Start()
    {
        mission1.Show();

        mission2.Show();
    }
}
