namespace CTO.Price.Shared.Actor
{
    public class ActorCommand
    {
        public class ReplyIfReady
        {
            public static ReplyIfReady Instance = new ReplyIfReady();
        }
    }
    
    public class ActorStatus
    {
        public class Ready
        {
            public static Ready Instance = new Ready();
        }
        
        public class Complete
        {
            public static Complete Instance = new Complete();
        }
    }
}