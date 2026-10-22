using System;

/// <summary>
/// Anything the menu logic can put on screen: a Canvas panel today, an in-world object later
/// (a ticket booth the camera moves to, a book that turns its pages). Showing and hiding may
/// animate, so the presenter waits for the callback before it moves on, and ignores input until then.
/// </summary>
public interface IMenuView
{
    /// <summary>Brings the view up; <paramref name="onShown"/> runs once it is fully shown and usable.</summary>
    void Show(Action onShown = null);

    /// <summary>Takes the view away; <paramref name="onHidden"/> runs once it is fully gone.</summary>
    void Hide(Action onHidden = null);
}
