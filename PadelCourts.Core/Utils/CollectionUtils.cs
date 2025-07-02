namespace WebApplication1.Utils;

public static class CollectionUtils
{
    public static IEnumerable<IEnumerable<T>> CreateBatches<T>(IEnumerable<T> source, int batchSize)
    {
        var batch = new List<T>(batchSize);
    
        foreach (var item in source)
        {
            batch.Add(item);
        
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }
    
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
}