using System.Reflection;

namespace JetIslandArchipelago.Modifiers;

public class Modifier(object obj, FieldInfo booleanEnabled)
{
    protected readonly PlayerBody playerBody = PlayerBody.localPlayer;
    public bool Enabled { get; private set; }

    public virtual void Initialize() { }

    public virtual void Enable()
    {
        booleanEnabled.SetValue(obj, Enabled=true); 
    }

    public virtual void Disable()
    {
        
        booleanEnabled.SetValue(obj, Enabled=false); 
    }
}