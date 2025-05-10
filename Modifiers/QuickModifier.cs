using System;
using System.Reflection;

namespace JetIslandArchipelago.Modifiers;

public class QuickModifier(object obj, FieldInfo booleanEnabled,
    Action<PlayerBody> onEnable, Action<PlayerBody> onDisable) 
    : Modifier(obj, booleanEnabled)
{

    public override void Enable()
    {
        base.Enable();
        onEnable(playerBody);   
    }

    public override void Disable()
    {
        base.Disable();
        onDisable(playerBody);
    }
}