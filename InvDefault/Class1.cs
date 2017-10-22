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
        .Ellipse(Colour.FromHSV((double)210, (double)0.88, (double)1.0), Colour.White, 2, new Point(100,100), new Point(20,20) )
        .Grow(600, 5)
        .FadeOut(5);

      Stopwatch stopwatch = null;

      canvas.DrawEvent += DC =>
      {
        if (stopwatch == null)
          stopwatch = Stopwatch.StartNew();

        test.Run(DC, stopwatch.Elapsed.TotalSeconds);
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

    public static float Lerp(float a, float b, float f)
    {
      return (a * (1.0f - f)) + (b * f);
    }

    public Animator CreateEllipse(Colour fill, Colour stroke, int strokeWidth, Point position, Point size)
    {
      actions = new List<Action>();
      State = States.created;
      _drawType = DrawType.ellipse;
      EllipseStruct ellipseStruct = new EllipseStruct(fill, stroke, strokeWidth, position, size, DateTime.UtcNow);
      AnimationStates.EllipseStruct = ellipseStruct;
      return new Animator();
    }

    public Animator Type(DrawType drawType)
    {
      _drawType = drawType;
      return this;
    }


    // chaining functions

    public Animator FadeOut(int seconds)
    {
      actions.Add(OnFadeOut);
      return this;

      void OnFadeOut()
      {
        double diff;
        if (_timeDelta > seconds)
        {
          var mult = (int)(_timeDelta / seconds);
          diff = _timeDelta - (mult * seconds);
        }
        else
        {
          diff = _timeDelta;
        }
        var perc = diff / (double)seconds;
        var value = Lerp(1.0f, 0f, (float)perc);
        Debug.WriteLine($"value {(float)value} {(float)perc} {_timeDelta} > {diff}");
        AnimationStates.EllipseStruct.Fill = Colour.FromHSV((double)210, (double)0.8, (double)value);
      }

    }

    public Animator Grow(int maxsize, int seconds)
    {
      actions.Add(OnGrow);
      return this;

      void OnGrow()
      {
        var currentSize = AnimationStates.EllipseStruct.Size;
        if (currentSize.X > maxsize)
        {
          currentSize = AnimationStates.EllipseStruct.OriginalSize;
          AnimationStates.EllipseStruct.Start = DateTime.UtcNow;
        }
        double diff;
        if (_timeDelta > seconds)
        {
          var mult = (int)(_timeDelta / seconds);
          diff = _timeDelta - (mult * seconds);
        }
        else
        {
          diff = _timeDelta;
        }
        var perc = diff / (double)seconds;
        //var timeDelta = (int)(DateTime.UtcNow - AnimationStates.EllipseStruct.Start).TotalSeconds;
        //var elapsed = Stopwatch.Elapsed.TotalMilliseconds;
        //var perc = (float) Stopwatch.Elapsed.TotalSeconds / seconds;
        //var t = (float)Math.Sin(perc * Math.PI * 0.5f);
        //var t = 1f - (float) Math.Cos(perc * Math.PI * 0.5f);
        //var t = perc * perc;
        var t = (float)Math.Abs(perc);
        t = t * t * (3f - 2f * t);
        //t = t * t * t * (t * (6f * t - 15f) + 10f);
        var size = Lerp(20, 900, t);
        var newSize = new Point((int)size, (int)size);
        AnimationStates.EllipseStruct.Size = newSize;
      }
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
