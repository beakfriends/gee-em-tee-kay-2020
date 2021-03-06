using UnityEngine;
using System.Collections.Generic;

public class WorldMap : MonoBehaviour
{
    public WorldTile[,] tileGrid;
    public int tileSize = 0;
    public Vector3 topLeftCornerLocation = new Vector3();

    public Vector2Int GetSizeInTiles()
    {
        return new Vector2Int(tileGrid.GetLength(0), tileGrid.GetLength(1));
    }

    public Vector3 GetTilePos(int x, int y)
    {
        return topLeftCornerLocation + new Vector3(x*tileSize, 0f, y*tileSize);
    }
    public Vector3 GetTilePos(WorldTile tile)
    {
        return GetTilePos(tile.x, tile.z);
    }

    public bool IsValidLocation(int x, int y)
    {
        bool isValid = (x >= 0 && x < tileGrid.GetLength(0) && y >= 0 && y < tileGrid.GetLength(1));
        return isValid;
    }

    public bool HasObstacleAt(int x, int y, List<EntityType> obstacleTypes)
    {
        return HasObstacleAt(tileGrid[x,y], obstacleTypes);
    }

    public bool HasObstacleAt(WorldTile inTile, List<EntityType> obstacleTypes)
    {
        return inTile.HasObstacle(obstacleTypes);
    }

    public void AddInhabitant(int x, int y, BaseEntity newInhabitant)
    {
        AddInhabitant(tileGrid[x,y], newInhabitant);
    }

    public void AddInhabitant(WorldTile tile, BaseEntity newInhabitant)
    {
        tile.AddInhabitant(newInhabitant);
        if (newInhabitant)
        {
            newInhabitant.SetTile(tile);
        }
    }

    public void RemoveInhabitant(int x, int y, BaseEntity inhabitant)
    {
        RemoveInhabitant(tileGrid[x,y], inhabitant);
    }

    public void RemoveInhabitant(WorldTile tile, BaseEntity inhabitant)
    {
        tile.RemoveInhabitant(inhabitant);
    }

    public void InteractWith(int x, int y, BaseInteractParams interactParams)
    {
        InteractWith(tileGrid[x,y], interactParams);
    }

    public void InteractWith(WorldTile inTile, BaseInteractParams interactParams)
    {
        inTile.TriggerInteract(interactParams);
    }

    public BaseEntity CreateEntityAtLocation(GameObject entityPrefab, WorldTile inTile)
    {
        return CreateEntityAtLocation(entityPrefab, inTile.x, inTile.z);
    }

    public BaseEntity CreateEntityAtLocation(GameObject entityPrefab, int tileX, int tileY)
    {
        GameObject newObject = Instantiate
        (
            entityPrefab,
            GetTilePos(tileX, tileY),
            Quaternion.identity,
            transform
        ) as GameObject;
        BaseEntity entity = newObject.GetComponent<BaseEntity>();
        AddInhabitant(tileX, tileY, entity);
        Game.entities.RegisterNewEntity(entity);
        return entity;
    }

    public WorldTile GetTileInDirectionFrom(Direction dir, WorldTile inTile)
    {
        int x = inTile.x, y = inTile.z;
        if (dir == Direction.North)
        {
            y++;
        }
        else if (dir == Direction.East)
        {
            x++;
        }
        else if (dir == Direction.South)
        {
            y--;
        }
        else
        {
            x--;
        }

        if (!IsValidLocation(x, y))
        {
            return null;
        }

        return tileGrid[x,y];
    }

    public WorldTile FindEntityInNeighbours(EntityType entity, WorldTile location)
    {
        for (int i = -1; i <=1; i++)
        {
            for (int j = -1; j<=1; j++)
            {
                if (i==0 && j==0)
                {
                    continue;
                }

                if (i != 0 && j != 0)
                {
                    continue;
                }

                WorldTile thisTile = tileGrid[location.x+i, location.z+j];
                if (thisTile.HasInhabitantOfType(entity))
                {
                    return thisTile;
                }
            }
        }

        return null;
    }

    public Vector2Int? FindAvailableNeighbourTo(WorldTile inTile, List<EntityType> obstacleTypes)
    {
        int posX = inTile.x, posY = inTile.z;

        int x = posX-1, y = posY;
        if (IsValidLocation(x,y) && !HasObstacleAt(x,y, obstacleTypes))
        {
            return new Vector2Int(x,y);
        }

        x = posX+1;
        if (IsValidLocation(x,y) && !HasObstacleAt(x,y, obstacleTypes))
        {
            return new Vector2Int(x,y);
        }

        x = posX;
        y = posY-1;
        if (IsValidLocation(x,y) && !HasObstacleAt(x,y, obstacleTypes))
        {
            return new Vector2Int(x,y);
        }

        y = posY+1;
        if (IsValidLocation(x,y) && !HasObstacleAt(x,y, obstacleTypes))
        {
            return new Vector2Int(x,y);
        }

        return null;
    }

    public List<Vector2Int> GetAllObstacleLocations(List<EntityType> obstacleTypes)
    {
        List<Vector2Int> obstaclePositions = new List<Vector2Int>();

        for (int i = 0; i < tileGrid.GetLength(0); i++)
        {
            for (int j = 0; j < tileGrid.GetLength(1); j++)
            {
                if (HasObstacleAt(i,j, obstacleTypes))
                {
                    obstaclePositions.Add(new Vector2Int(i,j));
                }
            }
        }

        return obstaclePositions;
    }
}
