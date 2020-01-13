using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonInteractions : MonoBehaviour
{
    public Camera playerCamera;
    public MineQuest.World world;

    private GameObject marker;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            world.Interaction.HitBlock(playerCamera.ScreenPointToRay(Input.mousePosition));
        else if (Input.GetMouseButtonDown(1))
            world.Interaction.InsertBlock(playerCamera.ScreenPointToRay(Input.mousePosition), MineQuest.Block.Type.Redstone);

    }
}
