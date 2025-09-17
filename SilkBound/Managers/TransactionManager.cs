using System;
using System.Collections.Generic;
using System.Text;

namespace SilkBound.Managers
{
    public class TransactionManager
    {
        public static Dictionary<string, object> Transactions = new Dictionary<string, object>();

        public static void Promise(string transactionId, object transactionData)
        {
            Transactions[transactionId] = transactionData;
        }
        public static T? Fetch<T>(string transactionId) where T : class
        {
            if (Transactions.TryGetValue(transactionId, out object? data))
            {
                return data as T;
            }
            return null;
        }
        public static void Revoke(string transactionId)
        {
            Transactions.Remove(transactionId);
        }
    }
}
