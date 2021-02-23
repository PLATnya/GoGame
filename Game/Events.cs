using System.Collections.Generic;
using Game.Rendering;
namespace Game.Events
{
    public enum EventType
    {
        Press,
        Release
        
    }
    public struct Message
    {
        public Dictionary<string, int> Dict;
    }
    public abstract class BigBrother
    {
        internal abstract void OnNotify(ScreenObject obj, EventType eventType, Message data);
        public abstract BigBrother Next { get; set; }
    }
    public delegate void MouseEvent(int x, int y);
    public class OnPressMessage:BigBrother
    {
        private readonly MouseEvent _onPress;
        public OnPressMessage(MouseEvent onPress)
        {
            _onPress = onPress;
        }
        internal override void OnNotify(ScreenObject obj, EventType eventType,Message data)
        {
            if (eventType == EventType.Press)
            {
                int x = data.Dict["X"];
                int y = data.Dict["Y"];
                bool contains = obj.Contains(x,y);
                if (contains)
                {
                    _onPress(x,y);
                }
            }
        }
        public override BigBrother Next { get; set; }
    }
    public class OnReleaseMessage:BigBrother
    {
        private readonly MouseEvent _onRelease;
        public OnReleaseMessage(MouseEvent onRelease)
        {
            _onRelease = onRelease;
        }
        internal override void OnNotify(ScreenObject obj, EventType eventType,Message data)
        {
            if (eventType == EventType.Release)
            {
                int x = data.Dict["X"];
                int y = data.Dict["Y"];
                _onRelease(x,y);
            }
        }
        private BigBrother _next;
        public override BigBrother Next { get=>_next; set=>_next = value; }
    }
    public class NotifierSub
    {
        private ScreenObject Self;
        private BigBrother ObserverHead { get; set; }
        public NotifierSub(ScreenObject obj)
        {
            Self = obj;
        }
        public void AddObserver(BigBrother brother)
        {
            if(ObserverHead!=null)
                brother.Next = ObserverHead;
            ObserverHead = brother;
        }
        public void Notify(EventType eventType, Message data)
        {
            BigBrother observer = ObserverHead;
            while (true)
            {
                observer.OnNotify(Self,eventType,data);
                if(observer.Next != null)
                    observer = observer.Next;
                else break;
            }
        }
    }
}
