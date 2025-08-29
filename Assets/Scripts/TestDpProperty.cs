using TouchSocket.Core;

public class TestDpProperty : DependencyObject
{
    public static readonly DependencyProperty<string> ABCProperty = new DependencyProperty<string>("ABC", string.Empty);

    public void ClickDp()
    {
        UnityLog.Logger.Info(this.GetValue(ABCProperty));

        this.SetValue(ABCProperty, "asd");

        UnityLog.Logger.Info(this.GetValue(ABCProperty));
    }
}
