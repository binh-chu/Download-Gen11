using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lge.Tools.Download.Pages
{
    /// <summary>
    /// Anibox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Anibox : UserControl
    {
        public Anibox(double aParentWidth, double aParentHeight)
        {
            InitializeComponent();

            this._parentW = aParentWidth;
            this._parentH = aParentHeight;

            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;

            _aniStory = new Storyboard();
            
            _aniStory.Completed += (sc, ec) => {
                _aniStory.Stop();
                // Go again..
                RandomiseAndStart();
            };

            this.Loaded += (s, e) =>
            {
                RandomiseAndStart();
            };

        }

        double _parentW;
        double _parentH;
        Storyboard _aniStory;

        private void RandomiseAndStart()
        {
            _aniStory.Children.Clear();

            var aniX = new DoubleAnimation();
            var aniY = new DoubleAnimation();

            this.ellipse.Opacity = Randomise(0.1, 0.9);
            this.effect.Radius = Randomise(20.0, 30.0);

            double scale = Randomise(0.2, 1.0);
            this.transscale.ScaleX = this.transscale.ScaleY = scale;

            aniX.Duration = aniY.Duration = new Duration(new TimeSpan(0, 0, (int)Randomise(3, 10)));

            aniX.From = Randomise(0, _parentW - (scale * this.ellipse.ActualWidth)) - ((_parentW - (scale * this.ellipse.ActualWidth)) / 2);
            aniX.To = Randomise(0, _parentW - (scale * this.ellipse.ActualWidth)) - ((_parentW - (scale * this.ellipse.ActualWidth)) / 2);

            aniY.From = Randomise(0, _parentH - (scale * this.ellipse.ActualHeight)) - ((_parentH - (scale * this.ellipse.ActualHeight)) / 2);
            aniY.To = Randomise(0, _parentH - (scale * this.ellipse.ActualHeight)) - ((_parentH - (scale * this.ellipse.ActualHeight)) / 2);

            Storyboard.SetTargetName(aniX, "transmove");
            Storyboard.SetTargetName(aniY, "transmove");
            Storyboard.SetTargetProperty(aniX, new PropertyPath("X"));
            Storyboard.SetTargetProperty(aniY, new PropertyPath("Y"));

            _aniStory.Children.Add(aniX);
            _aniStory.Children.Add(aniY);
            _aniStory.Begin(this);
        }

        static Anibox()
        {
            _random = new Random((int)DateTime.Now.Ticks);
        }
        static Random _random;

        static double Randomise(double aLower, double aHigher)
        {
            return (aLower + (_random.NextDouble() * (aHigher - aLower)));
        }
        
    }
}
