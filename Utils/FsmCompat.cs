namespace GodhomeQoL.Utils
{
    internal static class FsmCompat
    {
        internal static FsmEvent AddGlobalTransition(PlayMakerFSM fsm, string globalEventName, string toState)
        {
            return AddGlobalTransition(fsm.Fsm, globalEventName, toState);
        }

        internal static void ChangeTransition(PlayMakerFSM fsm, string stateName, string eventName, string? toState)
        {
            if (string.IsNullOrEmpty(toState))
            {
                return;
            }

            string targetState = toState!;
            ChangeTransition(GetState(fsm, stateName), eventName, targetState);
        }

        internal static void RemoveGlobalTransition(PlayMakerFSM fsm, string globalEventName)
        {
            RemoveGlobalTransition(fsm.Fsm, globalEventName);
        }

        private static FsmEvent AddGlobalTransition(Fsm fsm, string globalEventName, string toState)
        {
            FsmEvent globalEvent = new(globalEventName) { IsGlobal = true };
            FsmTransition transition = new()
            {
                ToState = toState,
                ToFsmState = fsm.GetState(toState),
                FsmEvent = globalEvent
            };

            FsmTransition[] globals = fsm.GlobalTransitions ?? Array.Empty<FsmTransition>();
            fsm.GlobalTransitions = AddItemToArray(globals, transition);
            return globalEvent;
        }

        private static void ChangeTransition(FsmState? state, string eventName, string toState)
        {
            if (state == null)
            {
                return;
            }

            FsmTransition? transition = GetTransition(state, eventName);
            if (transition == null)
            {
                return;
            }

            transition.ToState = toState;
            transition.ToFsmState = state.Fsm.GetState(toState);
        }

        private static void RemoveGlobalTransition(Fsm fsm, string globalEventName)
        {
            FsmTransition[] globals = fsm.GlobalTransitions ?? Array.Empty<FsmTransition>();
            if (globals.Length == 0)
            {
                return;
            }

            fsm.GlobalTransitions = RemoveItemsFromArray(globals, transition => transition.EventName == globalEventName);
        }

        private static FsmState? GetState(PlayMakerFSM fsm, string stateName)
        {
            return GetItemFromArray(fsm.FsmStates, state => state.Name == stateName);
        }

        private static FsmTransition? GetTransition(FsmState state, string eventName)
        {
            return GetItemFromArray(state.Transitions, transition => transition.EventName == eventName);
        }

        private static T? GetItemFromArray<T>(T[] array, Func<T, bool> predicate) where T : class
        {
            foreach (T item in array)
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            return null;
        }

        private static T[] AddItemToArray<T>(T[] array, T item)
        {
            int length = array.Length;
            T[] newArray = new T[length + 1];
            Array.Copy(array, newArray, length);
            newArray[length] = item;
            return newArray;
        }

        private static T[] RemoveItemsFromArray<T>(T[] array, Func<T, bool> shouldRemove)
        {
            int removeCount = 0;
            foreach (T item in array)
            {
                if (shouldRemove(item))
                {
                    removeCount++;
                }
            }

            if (removeCount == 0)
            {
                return array;
            }

            T[] newArray = new T[array.Length - removeCount];
            int index = 0;
            foreach (T item in array)
            {
                if (!shouldRemove(item))
                {
                    newArray[index++] = item;
                }
            }

            return newArray;
        }
    }
}
