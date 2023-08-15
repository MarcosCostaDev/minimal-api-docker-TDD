using RinhaBackEnd.Dtos.Response;

namespace RinhaBackEnd.Extensions
{
    public static class QueueExtensions
    {
        public static IEnumerable<PersonResponse> Dequeue(this ConcurrentQueue<PersonResponse> queue, int quantity)
        {
            var itemsReleased = 0;
            while (quantity > itemsReleased && queue.TryDequeue(out var response))
            {
                itemsReleased++;
                yield return response!;
            }

            yield break;
        }
    }
}
