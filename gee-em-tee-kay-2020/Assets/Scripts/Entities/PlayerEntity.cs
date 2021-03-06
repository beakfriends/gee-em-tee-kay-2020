using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerInteractParams : BaseInteractParams
{
    public PlayerInteractParams(PlayerEntity player) :  base(EntityType.Player, player)
    { }

    public bool holdingWater = false;
    public Ancestor heldAncestor = null;
    public int eggsToLay = 0;
}

public class PlayerEntity : BaseEntity
{
    public static List<EntityType> obstacleTypes = new List<EntityType>();
    public Color debugColourWithWater;
    public Color debugColourWithAncestor;
    public Color debugColourNormal;

    [SerializeField]
    private int maxEggsLaidAtOnce = 1;
    [SerializeField]
    private Vector2Int lifeSpanRange;
    [SerializeField]
    private SkinnedMeshRenderer beardRenderer = null;
    [SerializeField]
    private SkinnedMeshRenderer eyeRenderer = null;
    [SerializeField]
    private GameObject water = null;
    [SerializeField]
    private GameObject waterSplash = null;
    [SerializeField]
    private List<EntityType> instanceObstacleTypes = new List<EntityType>();

    private int currentTimeStepsTillDeath = 0;
    private bool holdingWater = false;
    private Ancestor heldAncestor = null;
    private int eggsToLay = 0;
    private int lifespan = 0;

    //cache
    private Animator animator;
    private PlayerController playerController;

    public override void TriggerInteract(BaseInteractParams interactParams)
    {
        if (interactParams.interactingType == EntityType.Player)
        {
            OnInteractWithSelf();
        }

        base.TriggerInteract(interactParams);
    }

    public override void InteractionResult(BaseInteractedWithParams interactedWithParams) 
    {
        switch (interactedWithParams.typeInteractedWith)
        {
            case EntityType.Ancestor:
                OnAncestorInteractedWith(interactedWithParams.entityInteractedWith as Ancestor);
                break;
            case EntityType.Incubator:
                OnIncubatorInteractedWith(interactedWithParams as IncubatorInteractedWithParams);
                break;
            case EntityType.Water:
                OnWaterInteractedWith();
                break;
            case EntityType.WorldTree:
                OnWorldTreeInteractedWith(interactedWithParams as WorldTreeInteractedWithParams);
                break;
            case EntityType.Enemy_WeakMelee:
                Game.entities.StepTime();
                break;
        }
    }

    public override EntityType GetEntityType()
    {
        return EntityType.Player;
    }

    public override void StepTime()
    {
        currentTimeStepsTillDeath--;
        animator.SetFloat("Age", (float)(lifespan-currentTimeStepsTillDeath)/(float)lifespan);
        beardRenderer.SetBlendShapeWeight(0, ((float)(currentTimeStepsTillDeath)/(float)lifespan)*100f);
        eyeRenderer.SetBlendShapeWeight(0, ((float)(lifespan-currentTimeStepsTillDeath)/(float)lifespan)*100f);
        if (currentTimeStepsTillDeath == 0)
        {
            Game.entities.onTimeStepComplete += Die;
        }
    }

    void GatherAncestor(Ancestor inAncestor)
    {
        // Trigger animation
        StartCoroutine(SetLayerWeight(4f, 0.1f, 1, 1f));
        heldAncestor = inAncestor;
    }

    void BuryAncestor()
    {
        // Trigger animation
        Game.entities.UnregisterEntity(heldAncestor);
        heldAncestor = null;
    }

    void UseWater()
    {
        StartCoroutine(SetLayerWeight(0f, 0.5f, 2, 0f));
        animator.CrossFadeInFixedTime("SpitWater", 0.1f);
        GameObject go = Instantiate(waterSplash, transform.position, Quaternion.Euler(0f, GetComponent<PlayerEntity>().rotationSmoother.GetDesiredValue(), 0f));
        Destroy(go, 5f);
        // Trigger animation
        holdingWater = false;
        water.SetActive(false);
    }

    void GatherWater()
    {
        animator.CrossFadeInFixedTime("Drink", 0.1f);
        water.SetActive(true);
        StartCoroutine(SetLayerWeight(1f, 0.2f, 2, 1f));
        // Trigger animation
        holdingWater = true;
    }

    IEnumerator SetLayerWeight(float delay, float time, int layer, float weight)
    {
        yield return new WaitForSeconds(delay);

        float timeCounter = 0f;
        float initial = animator.GetLayerWeight(layer);
        while (timeCounter < time)
        {
            timeCounter += Time.deltaTime;
            animator.SetLayerWeight(layer, Mathf.Lerp(initial, weight, timeCounter/time));
            yield return null;
        }
    }

    void LayEggs()
    {
        // Trigger animation
        eggsToLay = 0;
    }

    void Relax()
    {
        // Trigger animation
        eggsToLay++;
        eggsToLay = Mathf.Min(eggsToLay, maxEggsLaidAtOnce);
    }

    void SpawnTowerFrom(Ancestor ancestor, int tileX, int tileY)
    {
        BuryAncestor();

        animator.CrossFadeInFixedTime("PlantAntler", 0.1f);

        StartCoroutine(MoveAsideAfter(ancestor, tileX, tileY));
    }

    IEnumerator MoveAsideAfter(Ancestor ancestor, int tileX, int tileY)
    {
        yield return new WaitForSeconds(0.9f);
        
        // Move out of square
        playerController.MoveAside();

        // Spawn tower
        GameObject towerPrefabToSpawn = TowerTypeSelector.TowerToSpawnFromAncestor(ancestor);
        Game.worldMap.CreateEntityAtLocation(towerPrefabToSpawn, tileX, tileY);
    }

    void Die(int timeStep)
    {
        StartCoroutine(Death());
        Game.entities.onTimeStepComplete -= Die;
    }

    void Awake()
    {
        playerController = GetComponentInChildren<PlayerController>();
        playerController.onInteractWith += InteractWith;
        lifespan = Random.Range(lifeSpanRange.x, lifeSpanRange.y + 1);
        currentTimeStepsTillDeath = lifespan;

        animator = GetComponentInChildren<Animator>();
        beardRenderer.SetBlendShapeWeight(0, ((float)(currentTimeStepsTillDeath)/(float)lifespan)*100f);

        obstacleTypes = instanceObstacleTypes;
    }

    void Update()
    {
        if (holdingWater)
        {
            debugColor = debugColourWithWater;
        }
        else if (heldAncestor)
        {
            debugColor = debugColourWithAncestor;
        }
        else
        {
            debugColor = debugColourNormal;
        }
    }

    void LateUpdate()
    {
        UpdateRotation();
    }

    

    void InteractWith(int x, int y)
    {
        PlayerInteractParams interactParams = new PlayerInteractParams(this);
        interactParams.holdingWater = holdingWater;
        interactParams.heldAncestor = heldAncestor;
        interactParams.eggsToLay = eggsToLay;
        interactParams.tileX = x;
        interactParams.tileY = y;

        Game.worldMap.InteractWith(x, y, interactParams);
    }

    void OnInteractWithSelf()
    {
        if (heldAncestor)
        {
            SpawnTowerFrom(heldAncestor, currentWorldTile.x, currentWorldTile.z);
            // Time step handled by move as part of placement
        }
        else
        {
            // Interacting with yourself is relaxing
            Relax();
            Game.entities.StepTime();
        }
    }

    void OnAncestorInteractedWith(Ancestor inAncestor)
    {
        GatherAncestor(inAncestor);
        Game.entities.StepTime();
    }

    void OnIncubatorInteractedWith(IncubatorInteractedWithParams incubatorParams)
    {
        if (incubatorParams.watered)
        {
            UseWater();
            Game.entities.StepTime();
        }
        else if (incubatorParams.fertilised)
        {
            LayEggs();
            Game.entities.StepTime();
        }
    }

    void OnWaterInteractedWith()
    {
        GatherWater();
        Game.entities.StepTime();
    }

    void OnWorldTreeInteractedWith(WorldTreeInteractedWithParams worldTreeParams)
    {
        if (worldTreeParams.buriedAncestor)
        {
            BuryAncestor();
            Game.entities.StepTime();
        }
        else if (worldTreeParams.watered)
        {
            UseWater();
            Game.entities.StepTime();
        }
    }

    IEnumerator Death()
    {
        playerController.canMove = false;
        // Trigger animation

        playerController.FaceDirection(Direction.South);
        animator.CrossFadeInFixedTime("Walk", 0.1f);
        yield return new WaitForSeconds(0.1f);
        animator.CrossFadeInFixedTime("Idle", 0.1f);

        yield return new WaitForSeconds(0.5f);


        animator.CrossFadeInFixedTime("Death", 0.1f);

        yield return new WaitForSeconds(0.1f);
        eyeRenderer.SetBlendShapeWeight(1, 100f);

        yield return new WaitForSeconds(1.5f);

        float timeCounter = 0f;
        float beardBlanketBlendTime = 1f;
        while (timeCounter < beardBlanketBlendTime)
        {
            timeCounter += Time.deltaTime;
            beardRenderer.SetBlendShapeWeight(1, ((float)(timeCounter)/(float)beardBlanketBlendTime)*100f);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Unregister entity from entities
        Game.entities.UnregisterEntity(this);

        // Drop held ancestor next to us
        if (heldAncestor)
        {
            // @TODO Check for collisions for Ancestor
            if (Game.worldMap.FindAvailableNeighbourTo(currentWorldTile, Ancestor.obstacleTypes) is Vector2Int tilePosition)
            {
                Game.worldMap.AddInhabitant(tilePosition.x, tilePosition.y, heldAncestor);
            }
            else
            {
                // Just destroy the ancestor
                Game.entities.UnregisterEntity(heldAncestor);
                Destroy(heldAncestor.gameObject);
            }
            heldAncestor = null;
        }

        // Remove ourselves from square
        Game.worldMap.RemoveInhabitant(currentWorldTile, this);

        // Spawn new Ancestor where we are
        Ancestor newAncestor = Game.worldMap.CreateEntityAtLocation(Game.entities.GetPrefab(EntityType.Ancestor), currentWorldTile) as Ancestor;

        // Reparent player model to ancestor

        // Notify Lifecycle script that we need a new character
        Game.lifeCycleManager.RegisterCharacterDeath(gameObject);
    }
}
