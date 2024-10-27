namespace Components;

using System;
using System.Collections.Generic;
using DefaultEcs;
using Godot;

/// <summary>
///   Entity that triggers various microbe event callbacks when things happens to it. This is mostly used for
///   connecting the player cell to the GUI and game stage.
/// </summary>
[JSONDynamicTypeAllowed]
public struct MulticellularEventCallbacks
{

    public Action<Entity, IHUDMessage>? OnNoticeMessage;

    /// <summary>
    ///   Called when the reproduction status of this microbe changes
    /// </summary>
    public Action<Entity, bool>? OnReproductionStatus;

    /// <summary>
    ///   Temporary callbacks can be deleted in certain situations (for example used when creating microbe colony
    ///   event forwarders which are destroyed when the colony is disbanded)
    /// </summary>
    public bool IsTemporary;
}

public static class MulticellularEventCallbackHelpers
{
    /// <summary>
    ///   Send a microbe notice message to the entity if possible
    /// </summary>
    /// <param name="entity">Entity to send the message to</param>
    /// <param name="message">The message text</param>
    /// <returns>True if sent, false if missing the component or callback</returns>
    public static bool SendNoticeIfPossible(this in Entity entity, LocalizedString message)
    {
        if (!entity.Has<MulticellularEventCallbacks>())
            return false;

        ref var callbacks = ref entity.Get<MulticellularEventCallbacks>();

        if (callbacks.OnNoticeMessage == null)
            return false;

        callbacks.OnNoticeMessage.Invoke(entity, new SimpleHUDMessage(message.ToString()));
        return true;
    }

    /// <summary>
    ///   Variant that uses a factory method that is only called if the message can be sent to generate the message
    /// </summary>
    public static bool SendNoticeIfPossible(this in Entity entity, Func<SimpleHUDMessage> messageFactory)
    {
        if (!entity.Has<MulticellularEventCallbacks>())
            return false;

        ref var callbacks = ref entity.Get<MulticellularEventCallbacks>();

        if (callbacks.OnNoticeMessage == null)
            return false;

        callbacks.OnNoticeMessage.Invoke(entity, messageFactory());
        return true;
    }

    /// <summary>
    ///   Variant that uses an always allocated HUD message
    /// </summary>
    public static bool SendNoticeIfPossible(this in Entity entity, SimpleHUDMessage message)
    {
        if (!entity.Has<MulticellularEventCallbacks>())
            return false;

        ref var callbacks = ref entity.Get<MulticellularEventCallbacks>();

        if (callbacks.OnNoticeMessage == null)
            return false;

        callbacks.OnNoticeMessage.Invoke(entity, new SimpleHUDMessage(message.ToString()));
        return true;
    }
}
