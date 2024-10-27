namespace Components;

using System;
using DefaultEcs;
using Godot;
using Systems;

/// <summary>
///   Control variables for specifying how a microbe wants to move / behave
/// </summary>
[JSONDynamicTypeAllowed]
public struct MulticellularControl
{
    /// <summary>
    ///   Movement mode (walking, swimming, etc.)
    /// </summary>
    public MovementMode MovementMode = MovementMode.Swimming;

    /// <summary>
    ///   The point towards which the microbe will move to point to
    /// </summary>
    public Vector3 LookAtPoint;

    /// <summary>
    ///   The direction the microbe wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection;

    /// <summary>
    ///   Whether this microbe cannot sprint
    /// </summary>
    public bool OutOfSprint;

    /// <summary>
    ///   Whether this microbe is currently sprinting
    /// </summary>
    public bool Sprinting;

    /// <summary>
    ///   Constructs an instance with a sensible <see cref="LookAtPoint"/> set
    /// </summary>
    /// <param name="startingPosition">World position this entity is starting at</param>
    public MulticellularControl(Vector3 startingPosition)
    {
        LookAtPoint = startingPosition + new Vector3(0, 0, -1);
        MovementDirection = new Vector3(0, 0, 0);
        Sprinting = false;
    }
}

public static class MulticellularControlHelpers
{

    /// <summary>
    ///   Sets creature speed straight forward.
    /// </summary>
    /// <param name="control">Control to hold commands.</param>
    /// <param name="speed">Speed at which to move.</param>
    public static void SetMoveSpeed(this ref MulticellularControl control, float speed)
    {
        control.MovementDirection = new Vector3(0, 0, -speed);
    }

    /// <summary>
    ///   Moves creature towards target position, even if that position is not forward.
    ///   This does NOT handle any turning. So this is basically cell drifting.
    /// </summary>
    /// <param name="control">Control to hold commands.</param>
    /// <param name="selfPosition">Position of microbe moving.</param>
    /// <param name="targetPosition">Vector3 that microbe will move towards.</param>
    /// <param name="speed">Speed at which to move.</param>
    public static void SetMoveSpeedTowardsPoint(this ref MulticellularControl control, ref WorldPosition selfPosition,
        Vector3 targetPosition, float speed)
    {
        var vectorToTarget = targetPosition - selfPosition.Position;

        // If already at target don't move anywhere
        if (vectorToTarget.LengthSquared() < MathUtils.EPSILON)
        {
            control.MovementDirection = Vector3.Zero;
            return;
        }

        // MovementDirection doesn't have to be normalized, so it isn't here
        control.MovementDirection = selfPosition.Rotation.Inverse() * vectorToTarget * speed;
    }
}
