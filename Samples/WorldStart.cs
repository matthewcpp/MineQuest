using System.IO;
using UnityEngine;

public class WorldStart : MonoBehaviour
{
    public string worldFile;
    public MineQuest.World world;
    public GameObject player;

    public MineQuest.WorldBuilder WorldBuilder { get; private set; }
    private Vector3Int worldMin = new Vector3Int(-4, 0, -4);
    private Vector3Int worldMax = new Vector3Int(4, 10, 4);

    private float buildStartTime;

    void Start()
    {
        player.GetComponent<CharacterController>().enabled = false;

        if (File.Exists(worldFile))
        {
            WorldReady();
        }
        else
        {
            WorldBuilder = new MineQuest.WorldBuilder();
            buildStartTime = Time.realtimeSinceStartup;
            WorldBuilder.Build(worldFile, worldMin, worldMax);
        }
    }

    void Update()
    {
        if (WorldBuilder != null)
        {
            if (WorldBuilder.CreatedChunks == WorldBuilder.TotalChunks)
            {
                var buildEndTime = Time.realtimeSinceStartup;
                Debug.Log(string.Format("Built {0} world chunks in {1} seconds", WorldBuilder.TotalChunks, buildEndTime - buildStartTime));
                WorldReady();
                WorldBuilder = null;
            }
        }
    }

    private void WorldReady()
    {
        world.Open(worldFile);

        // determine the correct initial position for the player
        var playerPos = player.transform.position;
        playerPos.y = world.GetWorldHeight(playerPos.x, playerPos.z) + 1;
        player.transform.position = playerPos;

        world.LoadInitial();
        player.GetComponent<CharacterController>().enabled = true;

        GameObject.Destroy(this.gameObject);
    }
}
