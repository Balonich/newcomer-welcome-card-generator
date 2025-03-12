namespace Common.BaseClasses.Messaging
{
    public static class QueueNames
    {
        // Exchange names
        public const string CardGenerationExchange = "card-generation-exchange";
        public const string ImageExchange = "image-exchange";

        // Queue names
        public const string CardGenerationQueue = "card-generation-queue";
        public const string ImageQueue = "image-queue";

        // Routing keys
        public const string CardGenerationRoutingKey = "newcomer.data";
        public const string ImageRoutingKey = "newcomer.image";
    }
}