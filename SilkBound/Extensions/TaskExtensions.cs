using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkBound.Extensions {
    public static class TaskExtensions {
        /// <summary>
        /// Shorthand to silence fire and forget warnings.
        /// </summary>
        public static void Void(this Task _) { }

        private static async Task AwaitAsync(this Task task)
        {
            await task.ConfigureAwait(false);
        }

        public static bool Await(this Task task)
        {
            Task t = Task.Run(async () => {
                await AwaitAsync(task);
            });
            t.Wait();
            return t.IsCompletedSuccessfully;
        }

        /// <summary>
        /// Calls <see cref="Await(Task)"/> on <paramref name="task"/> and returns whether the assigned <paramref name="result"/> exists and is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result">The <see cref="Task{TResult}.Result"/> of <paramref name="task"/></param>
        /// <returns><see langword="true"/> if <paramref name="task"/> completed successfully and <paramref name="result"/> is not <see langword="null"/>, otherwise <see langword="false"/></returns>
        public static bool WaitForResult<T>(this Task<T> task, [NotNullWhen(true)] out T? result)
        {
            if (task.Await())
            {
                result = task.Result;
                return result != null;
            }

            result = default;
            return false;
        }

        public static T AssertResult<T>(this Task<T> task)
        {
            if (task.Await())
            {
                return task.Result;
            }

            throw task.Exception;
        }

        public static T? WaitForNullableResult<T>(this Task<T> task)
        {
            if (task.Await())
            {
                return task.Result;
            }

            return default;
        }
    }
}
