using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionUIManager : MonoBehaviour
{
    public MissionUI mission1;
    public MissionUI mission2;
    public MissionUI bossMission;

    string[,] missionNames =
    {
        { "Eliminate Hostiles", "Forward Base Defense" },
        { "Secure Mineral Resources", "Escort Payload" },
        { "Destroy High-Value Target", "Resource Processing" }
    };

    int clearCount = 0;

    void Start()
    {
        ShowSector(1);    // 게임 시작 시 1섹터 미션 표시
    }

    public void ShowSector(int sector)
    {
        mission1.SetText(missionNames[sector - 1, 0]);
        mission2.SetText(missionNames[sector - 1, 1]);

        mission1.Show();
        mission2.Show();

        bossMission.Hide();
    }

    public void CompleteMission(int missionNumber)
    {
        if (missionNumber == 1)
            mission1.Complete();
        else
            mission2.Complete();

        clearCount++;

        if (clearCount >= 2)
        {
            ShowBoss();
        }
    }

    void ShowBoss()
    {
        bossMission.SetText("보스 처치");
        bossMission.SetColor(Color.red);
        bossMission.Show();
    }

    public void CompleteBoss()
    {
        bossMission.Complete();
    }
}
