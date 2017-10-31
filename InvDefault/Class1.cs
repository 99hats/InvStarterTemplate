using Inv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Inv.Support;

namespace InvDefault
{
  public static class Shell
  {
    public static Folder cacheFolder { get; set; }

    public static void Install(Application Application)
    {
      Application.Title = "My Project";
      var Surface = Application.Window.NewSurface();
      Surface.Background.Colour = Colour.WhiteSmoke;
      cacheFolder = Application.Directory.NewFolder("cache");

      var main = new MainPage(Surface);
      Surface.Content = main;
      Application.Window.Transition(Surface);
    }
  }

  public static class Models
  {

    public static int last { get; set; } = 0;

    public class Asset
    {
      public string title { get; set; }
      public string uri { get; set; }
    }

    public static List<Asset> CreateAssets(int num)
    {
      List<Asset> result = new List<Asset>();
      for (int i = 0; i < num; i++)
      {
        result.Add(new Asset{ title = Colour.All[i + last].ToString(), uri = $"http://picsum.photos/300/200/?image={i+last}"});
      }
      last += num;
      if (last > 99) last = 0;
      return result;
    }
  }

  public class AssetPanel : Mimic<Button>
  {
    public Surface Surface { get; }
    public Models.Asset Asset { get; set; }

    public AssetPanel(Surface surface, Models.Asset asset)
    {
      Surface = surface;
      Base = surface.NewButton();
      Asset = asset;

      var title = surface.NewLabel();
      title.Text = Asset.title;
      title.Font.Size = 24;

      var graphic = new WebGraphic(surface, Asset.uri);
      graphic.Alignment.Stretch();

     
      var verticalDock = surface.NewVerticalDock();
      verticalDock.AddHeader(graphic);
      verticalDock.AddFooter(title);

      Base.Content = verticalDock;
    }

    public Size Size => Base.Size;
    public Alignment Alignment => Base.Alignment;
    public Background Background => Base.Background;
    public Margin Margin => Base.Margin;

  }

  internal sealed class MainPage : Mimic<Dock>
  {
    private int imageId;

    private List<Models.Asset> Assets { get; set; }

    public MainPage(Surface surface)
    {
      Base = surface.NewVerticalDock();

      Assets = Models.CreateAssets(38);

      HeroLayoutPool = new LayoutPool(surface, 1, 1, 1);
      ATFLayoutPool = new LayoutPool(surface, 3, 1, 3);
      BTFLayoutPool = new LayoutPool(surface, 33, 11, 3);

      ContentFrame = surface.NewFrame();
      Base.AddClient(ContentFrame);
      imageId = 0;

      surface.ArrangeEvent += UpdateUI;

      AddBelowTheFold(5, 33);

      void AddBelowTheFold(int start, int num)
      {
        for (int i = start; i < num; i++)
        {
          var asset = new AssetPanel(surface, Assets[i]);
          asset.Alignment.Stretch();
          asset.Background.Colour = Colour.Red; ;
          asset.Margin.Set(4);
          BTFLayoutPool.AddCellPanel(asset);
        }
      }

      // layout
      Scroll = surface.NewVerticalScroll();
      Scroll.Content = BTFLayoutPool.Table;
      ContentFrame.Transition(Scroll);

      void UpdateUI()
      {
        if (surface.Window.Width < 1) return;
        AddBelowTheFold(5,33);
        BTFLayoutPool.Reclaim();
        BTFLayoutPool.UpdateLayout();

        if (imageId > 99) imageId = 0;

        Debug.WriteLine($"{surface.Window.Application.Process.GetMemoryUsage().TotalMegabytes}mb {imageId}");
      }
    }

    public LayoutPool HeroLayoutPool { get; set; }

    public LayoutPool ATFLayoutPool { get; set; }

    private Frame ContentFrame { get; set; }
    private LayoutPool BTFLayoutPool { get; set; }
    private Scroll Scroll { get; }
  }

  public class LayoutPool
  {
    private readonly int Size;
    public LayoutPool(Surface surface, int size, int rows, int cols)
    {
      Surface = surface;

      Size = size;
      Rows = rows;
      Cols = cols;
      Frames = new List<Frame>(size);
      for (int i = 0; i < size; i++)
      {
        var frame = surface.NewFrame();
        Frames.Add(frame);
      }
      CellPanels = new List<Panel>(NextPrime(size * 2));
      RowDocks = new List<Dock>(NextPrime(rows));
      for (int i = 0; i < rows; i++)
      {
        var dock = surface.NewHorizontalDock();
        RowDocks.Add(dock);
      }
      BuildTable();
    }

    private List<Panel> CellPanels              { get; }
    private int         Cols                    { get; }
    private List<Frame> Frames                  { get; }
    private List<Dock>  RowDocks                { get; }
    private int         Rows                    { get; }
    private Surface     Surface                 { get; }
    public  Table       Table                   { get; set; }

    public void AddCellPanel(Panel cell) => CellPanels.Insert(0, cell);

    public void BuildTable()
    {
      Table = Surface.NewTable();
      var tableColumn = Table.AddColumn();
      tableColumn.Star();

      for (int r = 0; r < Rows; r++)
      {
        var tableRow = Table.AddRow();
        tableRow.Auto();
        var row = RowDocks[r];
        for (int c = 0; c < Cols; c++)
          row.AddClient(Frames[r * Cols + c]);
        Table.GetCell(0, r).Content = row;
      }
    }

    public void Reclaim()
    {
      if (CellPanels.Count >= Size * 2)
      {
        Debug.WriteLine($"Reclaiming at {CellPanels.Count}");
        //dispose
        for (int i = 33; i < CellPanels.Count - 33; i++)
          (CellPanels.ElementAt(i) as IDisposable)?.Dispose();
        //deref
        CellPanels.RemoveRange(Size, CellPanels.Count - Size);
        //reclaim
        Surface.Window.Application.Process.MemoryReclamation();
      }
    }

    public void UpdateLayout()
    {
      for (int i = 0; i < Size; i++)
      {
        var frame = Frames[i];
        var cell = CellPanels[i];
        frame.Transition(cell).Fade();
      }
    }
    private static int NextPrime(int a)
    {
      while (true) { a++; if (a < 2) continue; if (a == 2) break; if (a % 2 == 0) continue; bool flag = false; for (int i = 3; (i * i) <= a; i += 2) { if (a % i == 0) { flag = true; break; } } if (!flag) break; }
      return a;
    }
  }

  public class WebGraphic : Mimic<Graphic>, IDisposable

  {
    public WebGraphic(Surface surface, string uri)

    {
      var application = surface.Window.Application;
      Base = surface.NewGraphic();
      var cacheFileName = GetMD5Hash(uri) + ".jpg";
      var cache = Shell.cacheFolder.NewFile(cacheFileName);

      if (cache.Exists())

      {
        var thebytes = cache.ReadAllBytes();
        var image = new Image(thebytes, ".jpg");
        Base.Image = image;
        var dimensions = application.Graphics.GetDimension(Base.Image);
        aspectRatio = dimensions.Height / (float)dimensions.Width;
        var FadeInAnimation = surface.NewAnimation();
        var target = FadeInAnimation.AddTarget(Base);
        target.FadeOpacityIn(TimeSpan.FromSeconds(1));
        FadeInAnimation.Start();
        return;
      }

      surface.Window.RunTask(Thread =>

      {
        using (var download = application.Web.GetDownload(new Uri(uri)))
        using (var memoryStream = new MemoryStream((int)download.Length))
        {
          download.Stream.CopyTo(memoryStream);
          memoryStream.Flush();
          var image = new Image(memoryStream.ToArray(), ".png");
          var newCacheFile = Shell.cacheFolder.NewFile(cacheFileName);
          newCacheFile.WriteAllBytes(memoryStream.ToArray());
          Thread.Post(() =>
          {
            Base.Image = image;
            var dimensions = application.Graphics.GetDimension(Base.Image);
            aspectRatio = dimensions.Height / dimensions.Width;
            Base.Readjust();
          });
        }
      });
    }

    public Alignment Alignment => Base.Alignment;
    public double? aspectRatio { get; set; }
    public Background Background => Base.Background;
    public Margin Margin => Base.Margin;
    public Size Size => Base.Size;
    public static String GetMD5Hash(String TextToHash)

    {
      MD5 md5 = new MD5CryptoServiceProvider();
      byte[] textToHash = Encoding.Default.GetBytes(TextToHash);
      byte[] result = md5.ComputeHash(textToHash);
      return BitConverter.ToString(result);
    }

    public int aspectHeight(int width) => (int)(aspectRatio * width);

    public void Dispose()
    {
      Base.Surface.Window.Call(() => { Base.Image = null; });
    }
  }
}
