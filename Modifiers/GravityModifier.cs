using System.Reflection;

namespace JetIslandArchipelago.Modifiers;

public class GravityModifier(object obj, FieldInfo booleanEnabled, float gravity)
    : Modifier(obj, booleanEnabled)
{
    private float _defaultGravity;

    public override void Initialize()
    {
        _defaultGravity = playerBody.movement.gravity;
    }

    public override void Enable()
    {
        base.Enable();
        playerBody.movement.gravity = gravity;
    }

    public override void Disable()
    {
        base.Disable();
        playerBody.movement.gravity = _defaultGravity;
    }
}