using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;

namespace CoreTray.Views;

public sealed partial class WelcomeDialog : ContentDialog
{
    private int _currentStep = 1;

    public WelcomeDialog()
    {
        InitializeComponent();
        
        // Set XamlRoot to the MainWindow's content
        if (App.MainWindow.Content != null)
        {
            XamlRoot = App.MainWindow.Content.XamlRoot;
        }
        
        UpdateStepVisibility();
        
        // Animate in the first step
        AnimateStepIn(Step1Panel);
    }

    private void PrimaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Prevent dialog from closing
        args.Cancel = true;
        
        // Animate out current step
        AnimateStepOut(Step1Panel, () =>
        {
            // Move to next step
            _currentStep++;
            UpdateStepVisibility();
            
            // Animate in new step
            AnimateStepIn(Step2Panel);
        });
    }

    private void SecondaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Allow dialog to close (Get Started on step 2)
    }

    private void UpdateStepVisibility()
    {
        if (_currentStep == 1)
        {
            Step1Panel.Visibility = Visibility.Visible;
            Step2Panel.Visibility = Visibility.Collapsed;
            PrimaryButtonText = "Next";
            IsPrimaryButtonEnabled = true;
            SecondaryButtonText = "";
            
            UpdateProgressIndicators(1);
        }
        else if (_currentStep == 2)
        {
            Step1Panel.Visibility = Visibility.Collapsed;
            Step2Panel.Visibility = Visibility.Visible;
            PrimaryButtonText = ""; 
            IsPrimaryButtonEnabled = false;
            IsSecondaryButtonEnabled = true;
            SecondaryButtonText = "Get Started";

            UpdateProgressIndicators(2);
        }
    }

    private void UpdateProgressIndicators(int step)
    {
        try
        {
            var accentBrush = (Brush)App.Current.Resources["AccentFillColorDefaultBrush"];
            var defaultBrush = (Brush)App.Current.Resources["ControlStrokeColorDefaultBrush"];
            
            Step1Indicator.Fill = step == 1 ? accentBrush : defaultBrush;
            Step2Indicator.Fill = step == 2 ? accentBrush : defaultBrush;
            
            // Animate the active indicator with scale
            var activeIndicator = step == 1 ? Step1Indicator : Step2Indicator;
            
            var scaleTransform = new ScaleTransform
            {
                CenterX = 6,
                CenterY = 6
            };
            activeIndicator.RenderTransform = scaleTransform;
            
            var storyboard = new Storyboard();
            
            var scaleXAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleXAnimation, scaleTransform);
            Storyboard.SetTargetProperty(scaleXAnimation, "ScaleX");
            
            var scaleYAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleYAnimation, scaleTransform);
            Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");
            
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating progress indicators: {ex.Message}");
        }
    }

    private void AnimateStepIn(UIElement element)
    {
        try
        {
            element.Opacity = 0;
            
            var storyboard = new Storyboard();
            
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, element);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");
            storyboard.Children.Add(fadeIn);
            
            if (element.RenderTransform is TranslateTransform transform)
            {
                var slideIn = new DoubleAnimation
                {
                    From = 30,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(slideIn, transform);
                Storyboard.SetTargetProperty(slideIn, "Y");
                storyboard.Children.Add(slideIn);
            }
            
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error animating step in: {ex.Message}");
            element.Opacity = 1; // Fallback: just show it
        }
    }

    private void AnimateStepOut(UIElement element, Action onComplete)
    {
        try
        {
            var storyboard = new Storyboard();
            
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeOut, element);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");
            storyboard.Children.Add(fadeOut);
            
            if (element.RenderTransform is TranslateTransform transform)
            {
                var slideOut = new DoubleAnimation
                {
                    From = 0,
                    To = -30,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(slideOut, transform);
                Storyboard.SetTargetProperty(slideOut, "Y");
                storyboard.Children.Add(slideOut);
            }
            
            storyboard.Completed += (s, e) => onComplete?.Invoke();
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error animating step out: {ex.Message}");
            onComplete?.Invoke(); // Fallback: just proceed
        }
    }
}
