using UnityEngine;

abstract class BaseGameState
{
    public virtual void Init() {}
    public abstract void EnterState();
    public abstract void LeaveState();
}

class GameState_Playing : BaseGameState
{
    private WorldGenerator worldGenerator = null;

    public override void Init()
    {
        worldGenerator = GameObject.Find("World").GetComponent<WorldGenerator>();
    }

    public override void EnterState()
    {
        if (worldGenerator)
        {
            worldGenerator.GenerateWorld(Game.entities.GetPrefab(EntityType.WorldTree));
        }
    }

    public override void LeaveState()
    {
        if (worldGenerator)
        {
            worldGenerator.ClearWorldTiles();
        }
        Game.entities.ClearAll();
    }
}

class GameState_GameOver : BaseGameState
{
    public override void EnterState()
    {
        // Bring up black screen and sad graphic
        Debug.Log("Game Over!");
    }

    public override void LeaveState()
    {
        // hide sad graphic
    }
}