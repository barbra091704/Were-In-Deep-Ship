
using UnityEngine;
public abstract class MovementState
{
    
    public abstract void EnterState(PlayerMovement main);

    public abstract void UpdateState(PlayerMovement main, bool jumpPressed);

    public abstract void FixedUpdateState(PlayerMovement main, Vector2 input);

    

}