using Inv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace InvDefault
{
  public static class Shell
  {
    public static void Install(Inv.Application Application)
    {
      Application.Title = "My Project";

      var Surface = Application.Window.NewSurface();
      Surface.Background.Colour = Colour.WhiteSmoke;

      var pageDock = Surface.NewVerticalDock();

      var canvas = Surface.NewCanvas();
      canvas.Background.Colour = Colour.Black;

      DateTime? last = null;


      var test = Animator.CreateAnimator()
        .Ellipse(Colour.DodgerBlue, Colour.White, 2, new Point(100,100), new Point(20,20) )
        .Grow(900, 3);

      canvas.DrawEvent += DC =>
      {
        if (last == null)
        {
          last = DateTime.UtcNow;
          ;
          Debug.WriteLine("start");
          return;
        }
        var now = DateTime.UtcNow;
        var diff = ((TimeSpan)(now - last)).TotalSeconds;
        test.Run(DC, diff);
        last = now;
      };
      pageDock.AddClient(canvas);
      Surface.ComposeEvent += () => canvas.Draw();

      Surface.Content = pageDock;
      Application.Window.Transition(Surface);
    }
  }

  public class EllipseStruct
  {
    public EllipseStruct(Colour fill, Colour stroke, int strokeWidth, Point position, Point size, DateTime start)
    {
      Fill = fill;
      Stroke = stroke;
      StrokeWidth = strokeWidth;
      Position = position;
      Size = size;
      Start = start;
      OriginalSize = size;
    }
    public Colour Fill { get; set; }
    public Colour Stroke { get; set; }
    public int StrokeWidth { get; set; }
    public Point Position { get; set; }
    public Point Size { get; set; }
    public DateTime Start { get; set; }
    public Point OriginalSize { get; set; }

  }  

  public class Animator
  {
    public static DrawContract DrawContract;
    public enum DrawType
    {
      line, ellipse, rectangle, text, image
    }
    public enum States { created, running, cancelled, paused, completed }
    public static States State;

    public static Stopwatch Stopwatch { get; set; }

    public class AnimationStates
    {
      public static EllipseStruct EllipseStruct { get; set; }
    }

    private static double _timeDelta;
    public int count;

    public static List<Action> actions;
    private float _opacity;
    private int _seconds;
    private DrawType _drawType;
    private Colour _fillColour;
    private Colour _borderColour;
    private int _borderWidth;
    private Point _location;
    private Point _size;

    public static Animator CreateAnimator()
    {
      actions = new List<Action>();
      State = States.created;
      Stopwatch = new Stopwatch();
      Stopwatch.Start();
      return new Animator();
    }

    public Animator Type(DrawType drawType)
    {
      _drawType = drawType;
      return this;
    }
    // chaining functions
    public Animator Counter()
    {
      count = 0;
      actions.Add(UpdateCount);
      return this;
    }

    public Animator Ellipse(Colour fill, Colour stroke, int strokeWidth, Point position, Point size)
    {

      _drawType = DrawType.ellipse;
      EllipseStruct ellipseStruct = new EllipseStruct(fill, stroke, strokeWidth, position, size, DateTime.UtcNow);
      AnimationStates.EllipseStruct = ellipseStruct;
      return this;
    }

    public Animator Grow(int maxsize, int seconds)
    {
      
      actions.Add(()=>OnGrow(maxsize, seconds));
      return this;
    }

    public static void OnGrow(int maxsize, int seconds)
    {
      if (Stopwatch == null)
      {
        Stopwatch = new Stopwatch();
        Stopwatch.Start();
      }
      var currentSize = AnimationStates.EllipseStruct.Size;
      if (currentSize.X > maxsize)
      {
        currentSize = AnimationStates.EllipseStruct.OriginalSize;
        AnimationStates.EllipseStruct.Start = DateTime.UtcNow;
        Stopwatch = Stopwatch.StartNew();
      }
      var timeDelta = (int)(DateTime.UtcNow - AnimationStates.EllipseStruct.Start).TotalSeconds;
      var elapsed = Stopwatch.Elapsed.TotalMilliseconds;
      var size = currentSize.X +  (int)(Stopwatch.Elapsed.TotalMilliseconds / 1000) * seconds;
      //var size = currentSize.X + Math.Abs((int) (timeDelta * seconds));
      Debug.WriteLine($"{size} {currentSize.X} {timeDelta} * {seconds} | {elapsed}");
      var newSize = new Point(size, size);
      AnimationStates.EllipseStruct.Size = newSize;
    }

    public Animator FadeIn(int seconds)
    {
      _seconds = seconds;
      _opacity = 0.0f;
      actions.Add(OnFadeIn);
      return this;
    }

    private void OnFadeIn()
    {
      _opacity += _opacity * (float)_timeDelta;
    }

    private void UpdateCount()
    {
      count++;
    }

    // ending functions
    public Animator Run(DrawContract dc, double timeDelta)
    {
      if (State == States.created)
        State = States.running;
      _timeDelta = timeDelta;

      foreach (var _action in actions)
      {
        _action.Invoke();
      }

      Draw(dc);
      return this;
    }

    private void Draw(DrawContract drawContract)
    {
      switch (_drawType)
      {
          case DrawType.ellipse:
            var s = AnimationStates.EllipseStruct;
            drawContract.DrawEllipse(s.Fill, s.Stroke, s.StrokeWidth, s.Position, s.Size);
            break;
        default:
          break;
      }
    }

    public Animator Stop()
    {
      State = States.cancelled;
      return this;
    }
  }
}
