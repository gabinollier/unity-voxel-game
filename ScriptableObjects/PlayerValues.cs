using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu()]
public class PlayerValues : ScriptableObject
{
    [field: Header("Fly")]
    [field: SerializeField, ReadOnly] public float FlySpeed { get; private set; }
    [field:SerializeField] public int FlyDefaultSpeed { get; private set; }
    [field:SerializeField] public float FlySprintMultiplier { get; private set; }

    [field: Space, Header("Acceleration")]
    [field:SerializeField] public float BaseAcceleration { get; private set; }
    [field:SerializeField] public float SprintMultiplier { get; private set; }
    [field:SerializeField] public float SneakMultiplier { get; private set; }
    [field:SerializeField] public float AirMultiplier { get; private set; }

    [field: Space, Header("Friction")]
    [field: SerializeField] public float GroundFriction { get; private set; }
    [field: SerializeField] public float AirFriction { get; private set; }

    [field: Space, Header("Forces")]
    [field: SerializeField] public float JumpForce { get; private set; }
    [field: SerializeField] public float JumpBoostForce { get; private set; }

    [field: Space, Header("Camera")]
    [field: SerializeField] public float CameraSensibility { get; private set; }
    [field: SerializeField] public float BaseFOV { get; private set; }
    [field: SerializeField] public float SprintFOVMultiplier { get; private set; }
    [field: SerializeField] public float FOVChangeSmoothTime { get; private set; }

    [field: Space, Header("Hitbox")]
    [field: SerializeField] public float HitboxWidth { get; private set; }
    [field: SerializeField] public float HitboxHeight { get; private set; }
    [field: SerializeField] public float StepOffset { get; private set; }

    [field: Space, Header("Block Interactions")]
    [field: SerializeField] public float Reach { get; private set; }
    [field: SerializeField, ReadOnly] public float ReachSq { get; private set; }

    [field: Space]
    [field: SerializeField] public Gamemode DefaultGamemode { get; private set; }
    [field: SerializeField, ReadOnly] public Gamemode CurrentGamemode { get; private set; }

    public enum Gamemode
    {
        Survival, 
        Creative
    }

    void OnValidate()
    {
        FlySpeed = FlyDefaultSpeed;
        CurrentGamemode = DefaultGamemode;
        ReachSq = Reach * Reach;
    }

    [Command("flyspeed", "Changes creative fly speed.")]
    void FlyspeedCommand(int speed)
    {
        FlySpeed = speed;
    }


    [Command("gamemode")]
    void GamemodeCommand(Gamemode gamemode)
    {
        CurrentGamemode = gamemode;
    }

    [Command("reach")]
    void ReachCommand(int reach)
    {
        Reach = reach;
        ReachSq = reach * reach;
    }
}
