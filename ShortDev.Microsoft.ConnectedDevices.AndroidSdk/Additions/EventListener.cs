namespace Com.Microsoft.Connecteddevices
{
    public class EventListener<TSender, TArgs> : Java.Lang.Object, IEventListener where TSender : Java.Lang.Object where TArgs : Java.Lang.Object
    {
        public EventListener(IEvent @event){
            @event.Subscribe(this);
        }

        public void OnEvent(Java.Lang.Object sender, Java.Lang.Object args)
        {
            Event?.Invoke((TSender)sender, (TArgs)args);
        }

        public event EventDelegate Event;
        public delegate void EventDelegate(TSender sender, TArgs args);
    }

    public class EventListener : Java.Lang.Object, IEventListener
    {
        public EventListener(IEvent @event)
        {
            @event.Subscribe(this);
        }

        public void OnEvent(Java.Lang.Object sender, Java.Lang.Object args)
        {
            Event?.Invoke(sender, args);
        }

        public event EventDelegate Event;
        public delegate void EventDelegate(Java.Lang.Object sender, Java.Lang.Object args);
    }
}