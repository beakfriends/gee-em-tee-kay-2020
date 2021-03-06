using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public delegate void OnMoveTo(int x, int y);
    public OnMoveTo onMoveTo;

    public delegate void OnInteractWith(int x, int y);
    public OnInteractWith onInteractWith;

    [SerializeField, Socks.Field(category="Movement")]
    public Smoother rotationSmoother;

    [SerializeField, Socks.Field(category="Movement")]
    public float moveTime = 0.4f;

    [SerializeField, Socks.Field(category="Debug", readOnly=true)]
    public Direction facing;

    [SerializeField, Socks.Field(category="Debug", readOnly=true)]
    public bool canMove = true;

    private Animator animator;
    private PlayerEntity entity;
    public bool performingAction = false;

    public void MoveAside()
    {
        Vector2Int throwawayPosition = new Vector2Int();
        if (TryMove(Direction.North, false, ref throwawayPosition))
        {
            FaceDirection(Direction.South);
            return;
        }
        if (TryMove(Direction.East, false, ref throwawayPosition))
        {
            FaceDirection(Direction.West);
            return;
        }
        if (TryMove(Direction.South, false, ref throwawayPosition))
        {
            FaceDirection(Direction.North);
            return;
        }

        TryMove(Direction.West, false, ref throwawayPosition);
        FaceDirection(Direction.East);
    }

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        entity = GetComponentInChildren<PlayerEntity>();
    }

    void Update()
    {
        if (!canMove || performingAction)
        {
            return;
        }

        if (Game.input.GetButtonDown("Movement.North"))
        {
            PlayerMove(Direction.North);
        }
        else if (Game.input.GetButtonDown("Movement.East"))
        {
            PlayerMove(Direction.East);
        }
        else if (Game.input.GetButtonDown("Movement.South"))
        {
            PlayerMove(Direction.South);
        }
        else if (Game.input.GetButtonDown("Movement.West"))
        {
            PlayerMove(Direction.West);
        }
        else if (Game.input.GetButtonDown("Action.Interact"))
        {
            InteractInPlace();
        }
    }

    void UpdateRotation()
    {
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, rotationSmoother.Smooth(), transform.eulerAngles.z);
    }

    void InteractInPlace()
    {
        Vector2Int currentPosition = entity.GetPosition();
        onInteractWith?.Invoke(currentPosition.x, currentPosition.y);
    }

    public void FaceDirection(Direction dir)
    {
        entity.FaceDirection(dir);
    }

    // Player intiated move. Will fire onMoveTo callback if successful
    // Will try to interact otherwise
    void PlayerMove(Direction dir)
    {
        Vector2Int positionMovedTo = new Vector2Int();
        if (TryMove(dir, true, ref positionMovedTo))
        {
            onMoveTo?.Invoke(positionMovedTo.x, positionMovedTo.y);
        }
    }

    // Attempts to move somewhere. If we fail and are allowed to interact, do so
    bool TryMove(Direction dir, bool canInteract, ref Vector2Int finalPosition)
    {
        FaceDirection(dir);

        if (Game.worldMap.GetTileInDirectionFrom(dir, entity.GetTile()) is WorldTile destination)
        {
            if (CanMoveToValidLocation(destination.x, destination.z))
            {
                MoveToValidLocation(destination.x, destination.z);
                finalPosition = new Vector2Int(destination.x, destination.z);
                return true;
            }
            else if (canInteract)
            {
                onInteractWith?.Invoke(destination.x, destination.z);
            }
        }
        
        return false;
    }

    bool IsValidLocation(int x, int y)
    {
        return Game.worldMap.IsValidLocation(x,y);
    }

    public void MoveToValidLocation(int newX, int newY)
    {
        Game.worldMap.RemoveInhabitant(entity.GetTile(), entity);

        StartCoroutine(MoveCoroutine(transform.position, Game.worldMap.GetTilePos(newX, newY)));

        Game.worldMap.AddInhabitant(newX,newY, entity);
    }

    bool CanMoveToValidLocation(int x, int y)
    {
        // @TODO check for obstacles for player
        return !Game.worldMap.HasObstacleAt(x,y, PlayerEntity.obstacleTypes);
    }

    void OnDeath()
    {
        canMove = false;
    }

    IEnumerator MoveCoroutine(Vector3 oldPos, Vector3 newPos)
    {
        canMove = false;
        animator.CrossFadeInFixedTime("Walk", 0.1f);
        float timeCounter = 0f;
        while (timeCounter < moveTime)
        {
            timeCounter += Time.deltaTime;
            transform.position = Vector3.Lerp(oldPos, newPos, timeCounter/moveTime);

            yield return null;
        }

        animator.CrossFadeInFixedTime("Idle", 0.1f);

        canMove = true;
        Game.entities.StepTime();
    }
}
