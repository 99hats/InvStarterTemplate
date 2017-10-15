namespace InvMedia
{
  public static class Resources
  {
    static Resources()
    {
      global::Inv.Resource.Foundation.Import(typeof(Resources), "Resources.Resources1.InvResourcePackage.rs");
    }

    public static readonly ResourcesImages Images;
    public static readonly Resourcesaudio audio;
  }

  public sealed class ResourcesImages
  {
    public ResourcesImages() { }

    ///<Summary>41.5 KB</Summary>
    public readonly global::Inv.Image Logo;
  }

  public sealed class Resourcesaudio
  {
    public Resourcesaudio() { }

    ///<Summary>85.7 KB</Summary>
    public readonly global::Inv.Sound CoolSmsTone;
  }
}