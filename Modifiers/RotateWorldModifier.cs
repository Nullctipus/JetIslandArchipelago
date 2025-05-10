using System.Reflection;
using UnityEngine;

namespace JetIslandArchipelago.Modifiers;

public class RotateWorldModifier(object obj, FieldInfo booleanEnabled,Vector3 euler, Vector3 offset, Vector3 gravityDirection) 
    : Modifier(obj, booleanEnabled)
{

    public override void Enable()
    {
        base.Enable();
        playerBody.transform.rotation = Quaternion.Euler(euler);
        playerBody.transform.position += offset;
        playerBody.movement.gravityDirection = gravityDirection;
    }

    public override void Disable()
    {
        base.Disable();
        playerBody.transform.rotation = Quaternion.identity;
        playerBody.movement.gravityDirection = Vector3.down;
        
    }
}