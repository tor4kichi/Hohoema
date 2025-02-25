#nullable enable
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Controls.LiteInAppNotification;

[TemplatePart(Name = ContentPresenterPart, Type = typeof(ContentPresenter))]
public sealed partial class LiteInAppNotification : ContentControl
{
    private readonly List<LiteInAppNotificationPayload> _queue = new List<LiteInAppNotificationPayload>();

    private bool _nowPause = true;
    private bool _nowShowAnimationRunning;
    private bool _nowHideAnimationRunning;

    private ContentPresenter _contentProvider;
    private AnimationSet _showAnimationSet;
    private AnimationSet _hideAnimationSet;
    private AnimationScope _showingAnimation;

    private DateTime _prevGotAttentionAt = DateTime.MinValue;
    

    public LiteInAppNotification()
    {
        DefaultStyleKey = typeof(LiteInAppNotification);

        Window.Current.VisibilityChanged += Current_VisibilityChanged;
    }        

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_showAnimationSet != null)
        {
            _showAnimationSet.Started -= this.OnShowAnimationStarted;
            _showAnimationSet.Completed -= this.OnShowAnimationCompleted;
            _hideAnimationSet.Started -= this.OnHideAnimationStarted;
            _hideAnimationSet.Completed -= this.OnHideAnimationCompleted;
        }

        _contentProvider = (ContentPresenter)GetTemplateChild(ContentPresenterPart);
        _showAnimationSet = (AnimationSet)GetTemplateChild(ShowAnimationSetPart);
        _hideAnimationSet = (AnimationSet)GetTemplateChild(HideAnimationSetPart);
        _showingAnimation = (AnimationScope)GetTemplateChild(ShowingAnimationPart);

        if (_showAnimationSet != null)
        {
            _showAnimationSet.Started += this.OnShowAnimationStarted;
            _showAnimationSet.Completed += this.OnShowAnimationCompleted;
            _hideAnimationSet.Started += this.OnHideAnimationStarted;
            _hideAnimationSet.Completed += this.OnHideAnimationCompleted;
        }

        // 初期化完了まで一時停止扱いとしているので
        // TryShowNextではなくResumeを呼ぶ
        _nowPause = false;
        _hideAnimationSet.Start();
    }


    public void Show(object content, TimeSpan duration)
    {
        var payload = new LiteInAppNotificationPayload()
        {
            Content = content,
            Duration = duration,
        };
        
        _queue.Add(payload);
        TryShowNext();
    }


    internal void TryShowNext()
    {
        if (_contentProvider is null)
        {
            return;
        }

        if (!_nowShowAnimationRunning
            && !_nowHideAnimationRunning
            && !_nowPause
            )
        {
            var next = _queue.FirstOrDefault();
            if (next is null) { return; }

            _queue.Remove(next);

            TimeSpan duration = next.Duration;
            if (DateTime.Now - _prevGotAttentionAt > TimeSpan.FromSeconds(5))
            {
                duration += TimeSpan.FromSeconds(0.5);
            }

            _nowShowAnimationRunning = true;
            try
            {
                _showingAnimation.Duration = duration;

                switch (next.Content)
                {
                    case string text:
                        _contentProvider.ContentTemplate = null;
                        _contentProvider.Content = text;
                        break;
                    case UIElement element:
                        _contentProvider.ContentTemplate = null;
                        _contentProvider.Content = element;
                        break;
                    case DataTemplate dataTemplate:
                        // Without this check, the dataTemplate will fail to render.
                        // Why? Setting the ContentTemplate causes the control to re-evaluate it's Content value.
                        // When we set the ContentTemplate to the same instance of itself, we aren't actually changing the value.
                        // This means that the Content value won't be re-evaluated and stay null, causing the render to fail.
                        if (_contentProvider.ContentTemplate != dataTemplate)
                        {
                            _contentProvider.ContentTemplate = dataTemplate;
                            _contentProvider.Content = null;
                        }

                        break;
                    case object content:
                        _contentProvider.ContentTemplate = ContentTemplate;
                        _contentProvider.Content = content;
                        break;
                }

                _showAnimationSet.Start();
            }
            catch
            {
                _nowShowAnimationRunning = false;
            }
            finally
            {

            }
        }
    }

    public void Dismiss()
    {
        _nowPause = false;
        if (_nowShowAnimationRunning)
        {
            _showAnimationSet.Stop();
            _hideAnimationSet.Start();
        }
        else
        {
            _showAnimationSet.Stop();
            _hideAnimationSet.Start();
        }
    }

    private void Stop()
    {
        _showAnimationSet.Stop();
        _hideAnimationSet.Stop();
        _contentProvider.Opacity = 0.0;
    }

    private void StopAndClearQueue()
    {
        _queue.Clear();
        _showAnimationSet.Stop();
        _hideAnimationSet.Stop();
        _contentProvider.Opacity = 0.0;
    }

    private void Pause()
    {
        _showAnimationSet.Stop();
        _hideAnimationSet.Stop();
        
        _nowPause = true;
    }

    private void Resume()
    {
        _nowPause = false;

        if (_nowShowAnimationRunning)
        {
            _showAnimationSet.Start();
        }
        else if (_nowHideAnimationRunning)
        {
            _hideAnimationSet.Start();
        }
        else
        {
            TryShowNext();
        }
    }

    private void OnShowAnimationStarted(object sender, EventArgs e)
    {
        _nowShowAnimationRunning = true;
        _prevGotAttentionAt = DateTime.Now;
    }

    private void OnShowAnimationCompleted(object sender, EventArgs e)
    {
        _nowShowAnimationRunning = false;
        _hideAnimationSet.Start();
    }


    private void OnHideAnimationStarted(object sender, EventArgs e)
    {
        _nowHideAnimationRunning = true;
    }

    private void OnHideAnimationCompleted(object sender, EventArgs e)
    {
        _nowHideAnimationRunning = false;
        _prevGotAttentionAt = DateTime.Now;
        TryShowNext();
    }

    private void Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
    {
        if (_contentProvider is null)
        {
            return;
        }

        if (e.Visible)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }
}

public class LiteInAppNotificationPayload
{
    public object Content { get; set; }

    public TimeSpan Duration { get; set; }
}
