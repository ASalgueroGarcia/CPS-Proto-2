using System;

public static class DialogueAudioEvents
{
    public static event Action OnBarkSignDrop;
    public static event Action OnBarkSignRise;
    public static event Action<Speaker> OnNewLine;
    public static event Action OnChoiceMoved;
    public static event Action OnChoiceConfirmed;

    public static void TriggerBarkSignDrop() => OnBarkSignDrop?.Invoke();
    public static void TriggerBarkSignRise() => OnBarkSignRise?.Invoke();
    public static void TriggerNewLine(Speaker speaker) => OnNewLine?.Invoke(speaker);
    public static void TriggerChoiceMoved() => OnChoiceMoved?.Invoke();
    public static void TriggerChoiceConfirmed() => OnChoiceConfirmed?.Invoke();
}