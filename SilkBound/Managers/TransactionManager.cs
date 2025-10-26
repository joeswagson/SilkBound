using System.Collections.Generic;

namespace SilkBound.Managers
{
    public class TransactionManager
    {
        public static Dictionary<object, object> Transactions = [];

        public static T Promise<T>(object transactionId, T transactionData)
        {
            Transactions[transactionId] = transactionData!;
            return transactionData;
        }
        public static T? Fetch<T>(object transactionId)
        {
            if (Transactions.TryGetValue(transactionId, out object? data))
            {
                return (T?) data;
            }
            return default;
        }
        public static void Revoke(object transactionId)
        {
            Transactions.Remove(transactionId);
        }
    }
}
