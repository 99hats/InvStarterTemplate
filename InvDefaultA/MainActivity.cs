using Android.App;
using Application = Inv.Application;

namespace InvDefaultA
{
  [Activity(Label = "InvDefaultA", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
  public class MainActivity : Inv.AndroidActivity
  {
    protected override void Install(Application application)
    {
      InvDefault.Shell.Install(application);
    }
  }
}
