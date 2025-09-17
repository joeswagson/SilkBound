using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Managers
{
    public class TransactionManager
    {
        public static Dictionary<object, object> Transactions = new Dictionary<object, object>();

        public static void Promise(object transactionId, object transactionData)
        {
            Transactions[transactionId] = transactionData;
        }
        public static T? Fetch<T>(object transactionId)
        {
            if (Transactions.TryGetValue(transactionId, out object? data))
            {
                return (T) data;
            }
            return default;
        }
        public static void Revoke(object transactionId)
        {
            Transactions.Remove(transactionId);
        }
    }
}
