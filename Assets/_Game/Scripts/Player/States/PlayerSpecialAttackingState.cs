using UnityEngine;

public class PlayerSpecialAttackingState : PlayerStateBase
{
    public override void Tick()
    {
        if (Modules.Locomotion != null)
            Modules.Locomotion.SetMoveDirection(Vector3.zero);
    }
}
