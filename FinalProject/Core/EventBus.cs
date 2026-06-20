using System;
using System.Collections.Generic;

namespace FinalProject.Core;

public abstract class GameEvent { }


public class EventBus
{
    public static readonly EventBus Instance = new EventBus();
    
    // Maps event type -> list of handlers registered for that type.
    private readonly Dictionary<Type, List<Delegate>> _handlers =
        new Dictionary<Type, List<Delegate>>();

    private EventBus()
    {
    }

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEvent
    {
        Type key = typeof(TEvent);

        if (!_handlers.ContainsKey(key))
            _handlers[key] = new List<Delegate>();
        
        _handlers[key].Add(handler);
    }
    
    public void Publish<TEvent>(TEvent gameEvent) where TEvent : GameEvent
    {
        Type key = typeof(TEvent);

        if (!_handlers.TryGetValue(key, out List<Delegate> list))
            return;
        
        foreach (Delegate handler in new List<Delegate>(list))
            ((Action<TEvent>)handler)(gameEvent);
    }
}