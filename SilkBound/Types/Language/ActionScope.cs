using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SilkBound.Types.Language {
    public class ActionScope {
        /// <summary>
        /// Disconnects the current scope as an active listener on an action.
        /// https://stackoverflow.com/a/52738813
        /// </summary>
        /// <param name="obj">Object containing the action.</param>
        /// <param name="eventName">Field name of the action.</param>
        public static void Detach(object obj, string eventName)
        {
            var caller = new StackTrace().GetFrame(1).GetMethod();
            var type = obj.GetType();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                {
                    var handler = (field.GetValue(obj) as Delegate)?.GetInvocationList().FirstOrDefault(m => m.Method.Equals(caller));
                    if (handler != null)
                    {
                        type.GetEvent(eventName).RemoveEventHandler(obj, handler);
                        return;
                    }
                }
            }
        }
    }
}
