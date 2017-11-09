//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Orleans.Providers.MongoDB.UnitTest.Reminders
//{
//    public static class Utils
//    {
//        public static TimeSpan Multiply(this TimeSpan timeSpan, double value)
//        {
//            var ticksD = timeSpan.Ticks * value;
//            var ticks = checked((long) ticksD);
//            return TimeSpan.FromTicks(ticks);
//        }

//        public static void WaitWithThrow(this Task task, TimeSpan timeout)
//        {
//            if (!task.Wait(timeout))
//            {
//                throw new TimeoutException(string.Format("Task.WaitWithThrow has timed out after {0}.", timeout));
//            }
//        }

//        /// <summary>
//        ///     Returns a human-readable text string that describes an IEnumerable collection of objects.
//        /// </summary>
//        /// <typeparam name="T">The type of the list elements.</typeparam>
//        /// <param name="collection">The IEnumerable to describe.</param>
//        /// <returns>
//        ///     A string assembled by wrapping the string descriptions of the individual
//        ///     elements with square brackets and separating them with commas.
//        /// </returns>
//        public static string EnumerableToString<T>(IEnumerable<T> collection, Func<T, string> toString = null,
//            string separator = ", ", bool putInBrackets = true)
//        {
//            if (collection == null)
//            {
//                if (putInBrackets)
//                {
//                    return "[]";
//                }
//                else
//                {
//                    return "null";
//                }
//            }

//            var sb = new StringBuilder();
//            if (putInBrackets)
//            {
//                sb.Append("[");
//            }

//            var enumerator = collection.GetEnumerator();
//            var firstDone = false;
//            while (enumerator.MoveNext())
//            {
//                var value = enumerator.Current;
//                string val;
//                if (toString != null)
//                {
//                    val = toString(value);
//                }
//                else
//                {
//                    val = value == null ? "null" : value.ToString();
//                }

//                if (firstDone)
//                {
//                    sb.Append(separator);
//                    sb.Append(val);
//                }
//                else
//                {
//                    sb.Append(val);
//                    firstDone = true;
//                }
//            }
//            if (putInBrackets)
//            {
//                sb.Append("]");
//            }

//            return sb.ToString();
//        }

//        /// <summary>
//        ///     This will apply a timeout delay to the task, allowing us to exit early
//        /// </summary>
//        /// <param name="taskToComplete">The task we will timeout after timeSpan</param>
//        /// <param name="timeout">Amount of time to wait before timing out</param>
//        /// <exception cref="TimeoutException">If we time out we will get this exception</exception>
//        /// <returns>The completed task</returns>
//        internal static async Task WithTimeout(this Task taskToComplete, TimeSpan timeout)
//        {
//            if (taskToComplete.IsCompleted)
//            {
//                await taskToComplete;
//                return;
//            }

//            var timeoutCancellationTokenSource = new CancellationTokenSource();
//            var completedTask =
//                await Task.WhenAny(taskToComplete, Task.Delay(timeout, timeoutCancellationTokenSource.Token));

//            // We got done before the timeout, or were able to complete before this code ran, return the result
//            if (taskToComplete == completedTask)
//            {
//                timeoutCancellationTokenSource.Cancel();
//                // Await this so as to propagate the exception correctly
//                await taskToComplete;
//                return;
//            }

//            // We did not complete before the timeout, we fire and forget to ensure we observe any exceptions that may occur
//            taskToComplete.Ignore();
//            throw new TimeoutException(string.Format("WithTimeout has timed out after {0}.", timeout));
//        }
//    }
//}