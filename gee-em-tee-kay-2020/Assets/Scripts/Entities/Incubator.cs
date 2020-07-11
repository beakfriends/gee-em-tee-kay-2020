using UnityEngine;

public class Incubator : BaseEntity
{
    public Color debugColorWithEggs;
    public Color debugColorWhenWatered;

    private int numEggs = 0;
    private bool sufficientlyWatered = false;

    public override bool TriggerInteract(InteractParams interactParams)
    {
        if (numEggs > 0)
        {
            if (interactParams.holdingWater)
            {
                // Water plant
                sufficientlyWatered = true;
                interactParams.interactingCharacter.UseWater();
                return true;
            }
        }
        else if (interactParams.eggsToLay > 0)
        {
            // Lay egg
            numEggs = interactParams.eggsToLay;
            interactParams.interactingCharacter.LayEggs();
            return true;
        }
        return false;
    }

    void Update()
    {
        if (numEggs > 0)
        {
            if (sufficientlyWatered)
            {
                debugColor = debugColorWhenWatered;
            }
            else
            {
                debugColor = debugColorWithEggs;
            }
        }
    }
}
